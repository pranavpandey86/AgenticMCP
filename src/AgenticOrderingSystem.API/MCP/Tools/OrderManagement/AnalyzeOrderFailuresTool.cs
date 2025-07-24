using AgenticOrderingSystem.API.MCP.Tools.Base;
using AgenticOrderingSystem.API.MCP.Models;
using AgenticOrderingSystem.API.MCP.Interfaces;
using AgenticOrderingSystem.API.Services;
using AgenticOrderingSystem.API.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgenticOrderingSystem.API.MCP.Tools.OrderManagement
{
    /// <summary>
    /// AI-powered tool for analyzing order failures using Perplexity AI to provide intelligent suggestions based on team comparison data
    /// </summary>
    public class AnalyzeOrderFailuresTool : BaseMCPTool, IAgentTool
    {
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;
        private readonly IPerplexityAIService _perplexityAIService;

        public AnalyzeOrderFailuresTool(
            IOrderService orderService, 
            IUserService userService, 
            IPerplexityAIService perplexityAIService,
            ILogger<AnalyzeOrderFailuresTool> logger)
            : base(logger)
        {
            _orderService = orderService;
            _userService = userService;
            _perplexityAIService = perplexityAIService;
        }

        public override string Name => "analyze_order_failures";

        public override string Description => "Analyze order failures using AI to provide intelligent suggestions based on team comparison data";

        public override ParameterSchema Parameters => new()
        {
            Type = "object",
            Properties = new Dictionary<string, ParameterProperty>
            {
                ["orderId"] = new()
                {
                    Type = "string",
                    Description = "Order ID or Order Number to analyze (optional - if not provided, analyzes general failure patterns)",
                    Required = false
                },
                ["userId"] = new()
                {
                    Type = "string",
                    Description = "User ID to analyze failure patterns for (optional)",
                    Required = false
                }
            },
            Required = new List<string>()
        };

        protected override async Task<ToolResult> ExecuteInternalAsync(object parameters, CancellationToken cancellationToken)
        {
            return await ExecuteInternalAsync(parameters, cancellationToken, null);
        }

        private async Task<ToolResult> ExecuteInternalAsync(object parameters, CancellationToken cancellationToken, AgentToolContext? context)
        {
            try
            {
                var json = JsonSerializer.Serialize(parameters);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var toolParams = JsonSerializer.Deserialize<AnalyzeOrderFailuresParams>(json, options) ?? new AnalyzeOrderFailuresParams();

                // Step 1: Gather data for AI analysis
                var analysisData = await GatherOrderAnalysisDataAsync(toolParams, context);
                
                // Step 2: Send data to Perplexity AI for intelligent analysis
                var llmAnalysis = await AnalyzeWithPerplexityAIAsync(analysisData);
                
                // Step 3: Structure the AI response for the agent
                var structuredResponse = StructureAIResponseAsync(llmAnalysis, analysisData);
                
                _logger.LogInformation("Structured response created: {Response}", JsonSerializer.Serialize(structuredResponse));

                return ToolResult.CreateSuccess(structuredResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing order failures with AI");
                return ToolResult.CreateError("ANALYSIS_FAILED", $"Failed to analyze order: {ex.Message}");
            }
        }

        private async Task<OrderAnalysisData> GatherOrderAnalysisDataAsync(AnalyzeOrderFailuresParams toolParams, AgentToolContext? context = null)
        {
            var analysisData = new OrderAnalysisData();
            
            // Get the target order (rejected order to analyze)
            if (!string.IsNullOrEmpty(toolParams.OrderId))
            {
                var targetOrder = await _orderService.GetOrderByIdAsync(toolParams.OrderId);
                if (targetOrder == null)
                {
                    targetOrder = await _orderService.GetOrderByNumberAsync(toolParams.OrderId);
                }
                
                if (targetOrder != null && targetOrder.RequesterId != context?.UserId)
                {
                    return ToolResult.CreateError("UNAUTHORIZED", "You can only analyze your own orders");
                }
                
                analysisData.TargetOrder = targetOrder;
            }

            // Get team member successful orders for comparison
            if (analysisData.TargetOrder != null)
            {
                analysisData.TeamSuccessOrders = await GetTeamSuccessOrdersAsync(analysisData.TargetOrder);
            }

            return analysisData;
        }

        private async Task<List<Order>> GetTeamSuccessOrdersAsync(Order failedOrder)
        {
            var teamSuccessOrders = new List<Order>();
            
            try
            {
                // Get the requester to find their manager
                var requester = await _userService.GetUserByIdAsync(failedOrder.RequesterId);
                if (requester?.ManagerId == null) return teamSuccessOrders;
                
                // Find team members with same manager
                var allActiveUsers = await _userService.GetActiveUsersAsync(1, 1000);
                var teamMembers = allActiveUsers.Where(u => u.ManagerId == requester.ManagerId && u.Id != failedOrder.RequesterId).ToList();
                
                if (!teamMembers.Any()) return teamSuccessOrders;
                
                // Find successful orders from team members for similar products
                var teamMemberIds = teamMembers.Select(tm => tm.Id).ToList();
                
                var searchCriteria = new OrderSearchCriteria
                {
                    Status = "approved",
                    ProductCategory = failedOrder.ProductInfo.Category
                };
                
                var allOrders = await _orderService.SearchOrdersAsync(searchCriteria);
                
                teamSuccessOrders = allOrders.Where(o => 
                    teamMemberIds.Contains(o.RequesterId) &&
                    o.CreatedAt >= DateTime.UtcNow.AddDays(-90) // Last 90 days
                ).OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting team success orders for comparison");
            }
            
            return teamSuccessOrders;
        }

        private async Task<string> AnalyzeWithPerplexityAIAsync(OrderAnalysisData data)
        {
            var prompt = BuildAnalysisPrompt(data);
            
            var aiRequest = new AIRequest
            {
                SessionId = Guid.NewGuid().ToString(),
                Message = prompt,
                MaxTokens = 1500,
                Temperature = 0.1,
                Context = new AIContext
                {
                    OrderId = data.TargetOrder?.OrderNumber
                }
            };

            var response = await _perplexityAIService.SendMessageAsync(aiRequest);
            return response.Message;
        }

        private string BuildAnalysisPrompt(OrderAnalysisData data)
        {
            var prompt = new System.Text.StringBuilder();
            
            prompt.AppendLine("## ORDER FAILURE ANALYSIS REQUEST");
            prompt.AppendLine();
            
            if (data.TargetOrder != null)
            {
                prompt.AppendLine("### FAILED ORDER DETAILS:");
                prompt.AppendLine($"Order Number: {data.TargetOrder.OrderNumber}");
                prompt.AppendLine($"Product: {data.TargetOrder.ProductInfo.Name}");
                prompt.AppendLine($"Category: {data.TargetOrder.ProductInfo.Category}");
                prompt.AppendLine($"Requested Amount: ${data.TargetOrder.TotalAmount}");
                prompt.AppendLine($"Business Justification: {data.TargetOrder.BusinessJustification}");
                prompt.AppendLine($"Status: {data.TargetOrder.Status}");
                
                if (data.TargetOrder.ApprovalWorkflow?.History?.Any() == true)
                {
                    var rejectionAction = data.TargetOrder.ApprovalWorkflow.History.FirstOrDefault(h => h.Action == "reject");
                    if (rejectionAction != null)
                    {
                        prompt.AppendLine($"Rejection Reason: {rejectionAction.Reason}");
                        prompt.AppendLine($"Rejection Comments: {rejectionAction.Comments}");
                    }
                }
                prompt.AppendLine();
            }
            
            if (data.TeamSuccessOrders?.Any() == true)
            {
                prompt.AppendLine("### SUCCESSFUL TEAM ORDERS FOR COMPARISON:");
                foreach (var successOrder in data.TeamSuccessOrders.Take(3))
                {
                    prompt.AppendLine($"✅ Success Example {data.TeamSuccessOrders.IndexOf(successOrder) + 1}:");
                    prompt.AppendLine($"   Product: {successOrder.ProductInfo.Name}");
                    prompt.AppendLine($"   Amount: ${successOrder.TotalAmount}");
                    prompt.AppendLine($"   Justification: {successOrder.BusinessJustification}");
                    prompt.AppendLine($"   Approved Date: {successOrder.CreatedAt:yyyy-MM-dd}");
                    prompt.AppendLine();
                }
            }
            
            prompt.AppendLine("### ANALYSIS REQUEST:");
            prompt.AppendLine("1. Compare the failed order with successful team orders");
            prompt.AppendLine("2. Identify specific differences that caused the failure");
            prompt.AppendLine("3. Provide exact values that should be changed to fix the order");
            prompt.AppendLine("4. Include immediate actionable recommendations");
            prompt.AppendLine();
            prompt.AppendLine("Please provide your analysis in this format:");
            prompt.AppendLine("- **Failure Reason**: [specific reason]");
            prompt.AppendLine("- **Team Success Pattern**: [what worked for team members]");
            prompt.AppendLine("- **Recommended Fix**: [exact changes needed]");
            prompt.AppendLine("- **Immediate Actions**: [specific steps to take]");
            
            return prompt.ToString();
        }

        private object StructureAIResponseAsync(string aiResponse, OrderAnalysisData data)
        {
            // Parse AI response and structure it for the agent
            var hasImmediateActions = aiResponse.Contains("Immediate Actions") || aiResponse.Contains("immediateActions") || aiResponse.Contains("Recommended Fix");
            
            var structuredResponse = new Dictionary<string, object>
            {
                ["analysisContext"] = new Dictionary<string, object>
                {
                    ["scope"] = "ai_powered_analysis",
                    ["targetOrder"] = data.TargetOrder?.OrderNumber ?? "",
                    ["teamOrdersAnalyzed"] = data.TeamSuccessOrders?.Count ?? 0
                },
                ["failureAnalysis"] = new Dictionary<string, object>
                {
                    ["aiAnalysis"] = aiResponse,
                    ["commonFailureReasons"] = ExtractFailureReasonsFromAI(aiResponse)
                },
                ["successAnalysis"] = new Dictionary<string, object>
                {
                    ["teamPatterns"] = data.TeamSuccessOrders?.Select(o => new Dictionary<string, object>
                    {
                        ["product"] = o.ProductInfo.Name,
                        ["amount"] = o.TotalAmount,
                        ["justification"] = o.BusinessJustification
                    }).ToList() ?? new List<Dictionary<string, object>>()
                },
                ["recommendations"] = new Dictionary<string, object>
                {
                    ["immediateActions"] = hasImmediateActions ? ExtractImmediateActionsFromAI(aiResponse) : new List<string>(),
                    ["aiRecommendations"] = aiResponse,
                    ["confidenceScore"] = 0.85
                }
            };
            
            return structuredResponse;
        }

        private List<string> ExtractFailureReasonsFromAI(string aiResponse)
        {
            var reasons = new List<string>();
            
            // Simple extraction - in a real implementation, you might use more sophisticated parsing
            if (aiResponse.Contains("version") && (aiResponse.Contains("outdated") || aiResponse.Contains("discontinued")))
            {
                reasons.Add("OUTDATED VERSION REQUESTED");
            }
            if (aiResponse.Contains("justification") && aiResponse.Contains("insufficient"))
            {
                reasons.Add("INSUFFICIENT_JUSTIFICATION");
            }
            if (aiResponse.Contains("budget") || aiResponse.Contains("amount"))
            {
                reasons.Add("BUDGET_CONSTRAINTS");
            }
            
            return reasons;
        }

        private List<string> ExtractImmediateActionsFromAI(string aiResponse)
        {
            var actions = new List<string>();
            
            // Extract action items from AI response
            var lines = aiResponse.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("Action") || line.Contains("Fix") || line.Contains("Change") || line.Contains("Update"))
                {
                    actions.Add(line.Trim().TrimStart('-', '*', '•').Trim());
                }
            }
            
            // Ensure we have at least some actions
            if (!actions.Any())
            {
                actions.Add("Review order details based on AI analysis");
                actions.Add("Compare with successful team orders");
                actions.Add("Update order with recommended changes");
            }
            
            return actions.Take(5).ToList();
        }

        // Implement IAgentTool interface
        string IAgentTool.Name => "analyze_order_failures";
        string IAgentTool.Description => "Analyze order failures using AI to provide intelligent suggestions based on team comparison data";

        async Task<AgentToolResult> IAgentTool.ExecuteAsync(AgentToolContext context)
        {
            try
            {
                var parameters = new Dictionary<string, object>();
                
                if (context.Parameters.ContainsKey("orderId"))
                    parameters["orderId"] = context.Parameters["orderId"];
                if (context.Parameters.ContainsKey("userId"))
                    parameters["userId"] = context.Parameters["userId"];

                var mcpResult = await ExecuteInternalAsync(parameters, CancellationToken.None, context);
                
                _logger.LogInformation("MCP result success: {Success}, Data type: {DataType}, Data: {Data}", 
                    mcpResult.Success, 
                    mcpResult.Data?.GetType().Name ?? "null",
                    JsonSerializer.Serialize(mcpResult.Data));

                if (!mcpResult.Success)
                {
                    return new AgentToolResult
                    {
                        Success = false,
                        Error = mcpResult.Error?.Message ?? "Unknown error",
                        Output = "Failed to analyze order failures with AI"
                    };
                }

                var analysisData = mcpResult.Data as Dictionary<string, object> ?? new Dictionary<string, object>();
                
                _logger.LogInformation("After casting to Dictionary: {Data}", JsonSerializer.Serialize(analysisData));
                
                // Return the rich structured data directly from the MCP tool
                // This preserves all the AI analysis details, recommendations, and team comparisons
                var result = new AgentToolResult
                {
                    Success = mcpResult.Success,
                    Output = "AI analysis completed with detailed insights",
                    Data = analysisData
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing AnalyzeOrderFailuresTool in agent context");
                return new AgentToolResult
                {
                    Success = false,
                    Error = ex.Message,
                    Output = "Failed to analyze order failures with AI"
                };
            }
        }
    }

    // Data models for AI analysis
    public class OrderAnalysisData
    {
        public Order? TargetOrder { get; set; }
        public List<Order>? TeamSuccessOrders { get; set; }
    }

    public class AnalyzeOrderFailuresParams
    {
        public string? OrderId { get; set; }
        public string? ProductId { get; set; }
        public string? UserId { get; set; }
        public string AnalysisType { get; set; } = "all";
        public string TimeRange { get; set; } = "quarter";
    }
}
