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
    /// Tool for retrieving complete order details with context
    /// </summary>
    public class GetOrderDetailsTool : BaseMCPTool, IAgentTool
    {
        private const string ORDER_ID_PARAM = "orderId";
        private const string INCLUDE_HISTORY_PARAM = "includeHistory";
        private const string INCLUDE_APPROVALS_PARAM = "includeApprovals";
        private const string INCLUDE_USER_DETAILS_PARAM = "includeUserDetails";
        private const string REJECTED_STATUS = "rejected";
        
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;

        public GetOrderDetailsTool(IOrderService orderService, IUserService userService, ILogger<GetOrderDetailsTool> logger)
            : base(logger)
        {
            _orderService = orderService;
            _userService = userService;
        }

        public override string Name => "get_order_details";

        public override string Description => "Retrieve complete order information using either Order ID (GUID) or Order Number (e.g., REJ-2025-07-0001). Includes workflow details, approval history, and AI recommendations";

        public override ParameterSchema Parameters => new()
        {
            Type = "object",
            Properties = new Dictionary<string, ParameterProperty>
            {
                [ORDER_ID_PARAM] = new()
                {
                    Type = "string",
                    Description = "Order ID (GUID) or Order Number (e.g., REJ-2025-07-0001) to retrieve details for",
                    Required = true,
                    MinLength = 1
                },
                [INCLUDE_HISTORY_PARAM] = new()
                {
                    Type = "boolean",
                    Description = "Include complete approval history",
                    Required = false,
                    Default = true
                },
                [INCLUDE_APPROVALS_PARAM] = new()
                {
                    Type = "boolean",
                    Description = "Include detailed approval workflow information",
                    Required = false,
                    Default = true
                },
                [INCLUDE_USER_DETAILS_PARAM] = new()
                {
                    Type = "boolean",
                    Description = "Include detailed user information",
                    Required = false,
                    Default = false
                }
            },
            Required = new List<string> { ORDER_ID_PARAM }
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
                var toolParams = JsonSerializer.Deserialize<GetOrderDetailsParams>(json, options);

                if (toolParams == null || string.IsNullOrEmpty(toolParams.OrderId))
                {
                    return ToolResult.CreateError("INVALID_PARAMETERS", "OrderId is required");
                }

                // Get order details - try by ID first, then by order number
                var order = await _orderService.GetOrderByIdAsync(toolParams.OrderId);
                if (order == null)
                {
                    // Try to find by order number if ID lookup failed
                    order = await _orderService.GetOrderByNumberAsync(toolParams.OrderId);
                }
                
                if (order == null)
                {
                    return ToolResult.CreateError("ORDER_NOT_FOUND", $"Order with ID or number '{toolParams.OrderId}' not found");
                }

                if (order.RequesterId != context?.UserId)
                {
                    return ToolResult.CreateError("UNAUTHORIZED", "You can only access your own orders");
                }

                // Get user details if requested
                object? userDetails = null;
                if (toolParams.IncludeUserDetails == true)
                {
                    var user = await _userService.GetUserByIdAsync(order.RequesterInfo.UserId);
                    if (user != null)
                    {
                        userDetails = new
                        {
                            userId = user.Id,
                            employeeId = user.EmployeeId,
                            email = user.Email,
                            firstName = user.FirstName,
                            lastName = user.LastName,
                            department = user.Department,
                            role = user.Role,
                            managerId = user.ManagerId,
                            approvalAuthority = user.ApprovalAuthority
                        };
                    }
                }

                // Analyze workflow status
                var workflowAnalysis = AnalyzeWorkflowStatus(order);

                // Generate AI recommendations if order has issues
                var aiRecommendations = await GenerateRecommendations(order);

                var responseData = new
                {
                    order = new
                    {
                        orderId = order.Id,
                        orderNumber = order.OrderNumber,
                        userId = order.RequesterInfo.UserId,
                        userName = order.RequesterInfo.FullName,
                        productId = order.ProductId,
                        productName = order.ProductInfo.Name,
                        status = order.Status,
                        priority = order.Priority,
                        createdAt = order.CreatedAt,
                        updatedAt = order.UpdatedAt,
                        submittedAt = order.SubmittedAt,
                        customResponses = order.CustomResponses,
                        approvalWorkflow = toolParams.IncludeApprovals == true ? order.ApprovalWorkflow : null,
                        approvalHistory = toolParams.IncludeHistory == true ? order.ApprovalWorkflow.History : null,
                        businessJustification = order.BusinessJustification,
                        totalAmount = order.TotalAmount,
                        currency = order.Currency
                    },
                    userDetails,
                    workflowAnalysis,
                    aiRecommendations,
                    metadata = new
                    {
                        canBeModified = CanOrderBeModified(order),
                        nextPossibleActions = GetNextPossibleActions(order),
                        estimatedApprovalTime = EstimateApprovalTime(order),
                        riskFactors = IdentifyRiskFactors(order)
                    }
                };

                _logger.LogInformation("Retrieved order details for order {OrderId}", toolParams.OrderId);

                return ToolResult.CreateSuccess(responseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order details");
                return ToolResult.CreateError("RETRIEVAL_ERROR", $"Failed to retrieve order details: {ex.Message}");
            }
        }

        private object AnalyzeWorkflowStatus(Order order)
        {
            return new
            {
                currentStage = order.Status,
                currentStep = order.ApprovalWorkflow.CurrentStep,
                totalSteps = order.ApprovalWorkflow.TotalSteps,
                isComplete = order.ApprovalWorkflow.Status == "approved" || order.ApprovalWorkflow.Status == "rejected",
                nextApprover = GetNextApprover(order),
                requiresApproval = order.ApprovalWorkflow.IsRequired,
                timeInCurrentStage = DateTime.UtcNow - order.UpdatedAt,
                escalationEligible = ShouldEscalate(order)
            };
        }

        private string? GetNextApprover(Order order)
        {
            var nextApprover = order.ApprovalWorkflow.Approvers
                .Where(a => a.Status == "pending")
                .OrderBy(a => a.StepNumber)
                .FirstOrDefault();
            
            return nextApprover?.FullName;
        }

        private Task<List<object>> GenerateRecommendations(Order order)
        {
            var recommendations = new List<object>();

            // Generate recommendations based on order status
            switch (order.Status.ToLowerInvariant())
            {
                case REJECTED_STATUS:
                    // Check if this order has rejection details through approval history
                    var rejectionAction = order.ApprovalWorkflow.History.LastOrDefault(h => h.Action == "reject");
                    if (rejectionAction != null)
                    {
                        recommendations.Add(new
                        {
                            type = "rejection_analysis",
                            title = "Order Rejection Analysis",
                            description = "This order was rejected. Review the specific issues and consider resubmitting with corrections.",
                            priority = "high",
                            actionRequired = true,
                            details = new { rejectionReason = rejectionAction.Reason, comments = rejectionAction.Comments }
                        });
                    }
                    break;

                case "pending_l1":
                case "pending_l2":
                    recommendations.Add(new
                    {
                        type = "approval_pending",
                        title = "Approval Pending",
                        description = $"Order is awaiting approval at step {order.ApprovalWorkflow.CurrentStep}",
                        priority = "medium",
                        actionRequired = false,
                        estimatedTime = EstimateApprovalTime(order)
                    });
                    break;

                case "draft":
                    recommendations.Add(new
                    {
                        type = "incomplete_submission",
                        title = "Incomplete Order",
                        description = "Order is in draft status. Complete and submit for approval.",
                        priority = "medium",
                        actionRequired = true
                    });
                    break;
            }

            return Task.FromResult(recommendations);
        }

        private bool CanOrderBeModified(Order order)
        {
            return order.Status.ToLowerInvariant() switch
            {
                "draft" => true,
                "rejected" => true,
                _ => false
            };
        }

        private List<string> GetNextPossibleActions(Order order)
        {
            return order.Status.ToLowerInvariant() switch
            {
                "draft" => new List<string> { "edit", "submit", "delete" },
                "rejected" => new List<string> { "edit", "resubmit", "cancel" },
                "pending_l1" or "pending_l2" => new List<string> { "cancel", "check_status" },
                "approved" => new List<string> { "view_details" },
                _ => new List<string> { "view_details" }
            };
        }

        private string EstimateApprovalTime(Order order)
        {
            // Simple estimation logic - can be enhanced with historical data
            return order.ApprovalWorkflow.CurrentStep switch
            {
                1 => "24-48 hours",
                2 => "48-72 hours",
                _ => "Unknown"
            };
        }

        private List<string> IdentifyRiskFactors(Order order)
        {
            var risks = new List<string>();

            // Check for high-value orders
            if (order.TotalAmount > 10000)
            {
                risks.Add("High value order (>$10,000)");
            }

            // Check for urgent priority
            if (order.Priority?.ToLowerInvariant() == "urgent")
            {
                risks.Add("Urgent priority request");
            }

            // Check for risk score
            if (order.Metadata.RiskScore > 0.7m)
            {
                risks.Add("High risk score");
            }

            return risks;
        }

        private bool ShouldEscalate(Order order)
        {
            // Check if order has been in pending status for too long
            if (order.Status.ToLowerInvariant().Contains("pending"))
            {
                var timePending = DateTime.UtcNow - order.UpdatedAt;
                return timePending.TotalHours > 72; // Escalate after 72 hours
            }

            return false;
        }

        private sealed class GetOrderDetailsParams
        {
            [JsonPropertyName("orderId")]
            public string OrderId { get; set; } = string.Empty;
            
            [JsonPropertyName("includeHistory")]
            public bool? IncludeHistory { get; set; }
            
            [JsonPropertyName("includeApprovals")]
            public bool? IncludeApprovals { get; set; }
            
            [JsonPropertyName("includeUserDetails")]
            public bool? IncludeUserDetails { get; set; }
        }

        // Implement IAgentTool interface
        string IAgentTool.Name => "get_order_details";
        string IAgentTool.Description => "Retrieve complete order information using either Order ID (GUID) or Order Number (e.g., TEAM-SUCCESS-001). Includes workflow details, approval history, and AI recommendations";

        async Task<AgentToolResult> IAgentTool.ExecuteAsync(AgentToolContext context)
        {
            try
            {
                var parameters = new Dictionary<string, object>();
                
                if (context.Parameters.ContainsKey(ORDER_ID_PARAM))
                    parameters[ORDER_ID_PARAM] = context.Parameters[ORDER_ID_PARAM];
                if (context.Parameters.ContainsKey(INCLUDE_HISTORY_PARAM))
                    parameters[INCLUDE_HISTORY_PARAM] = context.Parameters[INCLUDE_HISTORY_PARAM];
                if (context.Parameters.ContainsKey(INCLUDE_APPROVALS_PARAM))
                    parameters[INCLUDE_APPROVALS_PARAM] = context.Parameters[INCLUDE_APPROVALS_PARAM];
                if (context.Parameters.ContainsKey(INCLUDE_USER_DETAILS_PARAM))
                    parameters[INCLUDE_USER_DETAILS_PARAM] = context.Parameters[INCLUDE_USER_DETAILS_PARAM];

                var mcpResult = await ExecuteInternalAsync(parameters, CancellationToken.None, context);

                if (!mcpResult.Success)
                {
                    return new AgentToolResult
                    {
                        Success = false,
                        Error = mcpResult.Error?.Message ?? "Unknown error",
                        Output = "Failed to retrieve order details"
                    };
                }

                // Properly serialize the data to ensure it can be cast to Dictionary<string, object>
                var jsonData = JsonSerializer.Serialize(mcpResult.Data);
                var deserializedData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new AgentToolResult
                {
                    Success = mcpResult.Success,
                    Output = "Order details retrieved successfully",
                    Data = deserializedData ?? new Dictionary<string, object>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetOrderDetailsTool in agent context");
                return new AgentToolResult
                {
                    Success = false,
                    Error = ex.Message,
                    Output = "Failed to retrieve order details"
                };
            }
        }
    }
}
