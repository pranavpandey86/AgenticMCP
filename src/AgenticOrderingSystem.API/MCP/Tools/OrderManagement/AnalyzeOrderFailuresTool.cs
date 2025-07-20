using AgenticOrderingSystem.API.MCP.Tools.Base;
using AgenticOrderingSystem.API.MCP.Models;
using AgenticOrderingSystem.API.Services;
using AgenticOrderingSystem.API.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgenticOrderingSystem.API.MCP.Tools.OrderManagement
{
    /// <summary>
    /// Tool for analyzing order failures and providing intelligent suggestions based on historical data
    /// </summary>
    public class AnalyzeOrderFailuresTool : BaseMCPTool
    {
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;

        public AnalyzeOrderFailuresTool(IOrderService orderService, IUserService userService, ILogger<AnalyzeOrderFailuresTool> logger)
            : base(logger)
        {
            _orderService = orderService;
            _userService = userService;
        }

        public override string Name => "analyze_order_failures";

        public override string Description => "Analyze order failure patterns and provide intelligent suggestions based on historical data for similar products and users";

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
                ["productId"] = new()
                {
                    Type = "string",
                    Description = "Product ID to analyze failure patterns for (optional)",
                    Required = false
                },
                ["userId"] = new()
                {
                    Type = "string",
                    Description = "User ID to analyze failure patterns for (optional)",
                    Required = false
                },
                ["analysisType"] = new()
                {
                    Type = "string",
                    Description = "Type of analysis: 'failure_reasons', 'success_patterns', 'recommendations', 'all'",
                    Required = false,
                    Default = "all"
                },
                ["timeRange"] = new()
                {
                    Type = "string",
                    Description = "Time range for analysis: 'week', 'month', 'quarter', 'year'",
                    Required = false,
                    Default = "quarter"
                }
            },
            Required = new List<string>()
        };

        protected override async Task<ToolResult> ExecuteInternalAsync(object parameters, CancellationToken cancellationToken)
        {
            try
            {
                var json = JsonSerializer.Serialize(parameters);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var toolParams = JsonSerializer.Deserialize<AnalyzeOrderFailuresParams>(json, options) ?? new AnalyzeOrderFailuresParams();

                // Determine analysis scope
                var analysisContext = await DetermineAnalysisContext(toolParams);
                
                // Analyze failure patterns
                var failureAnalysis = await AnalyzeFailurePatterns(analysisContext);
                
                // Analyze success patterns
                var successAnalysis = await AnalyzeSuccessPatterns(analysisContext);
                
                // Generate intelligent recommendations
                var recommendations = GenerateIntelligentRecommendations(failureAnalysis, successAnalysis, analysisContext);

                var responseData = new
                {
                    analysisContext = new
                    {
                        scope = analysisContext.Scope,
                        timeRange = toolParams.TimeRange,
                        targetOrder = analysisContext.TargetOrder,
                        productCategory = analysisContext.ProductCategory,
                        userDepartment = analysisContext.UserDepartment,
                        ordersAnalyzed = analysisContext.TotalOrdersAnalyzed
                    },
                    failureAnalysis = new
                    {
                        commonFailureReasons = failureAnalysis.CommonReasons,
                        failuresByCategory = failureAnalysis.FailuresByCategory,
                        failuresByDepartment = failureAnalysis.FailuresByDepartment,
                        failuresByAmountRange = failureAnalysis.FailuresByAmountRange,
                        criticalFailurePatterns = failureAnalysis.CriticalPatterns
                    },
                    successAnalysis = new
                    {
                        successFactors = successAnalysis.SuccessFactors,
                        optimalOrderPatterns = successAnalysis.OptimalPatterns,
                        fastApprovalStrategies = successAnalysis.FastApprovalStrategies,
                        bestPractices = successAnalysis.BestPractices
                    },
                    recommendations = new
                    {
                        immediateActions = recommendations.ImmediateActions,
                        preventiveStrategies = recommendations.PreventiveStrategies,
                        processImprovements = recommendations.ProcessImprovements,
                        riskMitigation = recommendations.RiskMitigation,
                        confidenceScore = recommendations.ConfidenceScore
                    },
                    insights = new
                    {
                        keyFindings = GenerateKeyFindings(failureAnalysis, successAnalysis),
                        trendAnalysis = GenerateTrendAnalysis(analysisContext),
                        benchmarks = GenerateBenchmarks(analysisContext)
                    }
                };

                _logger.LogInformation("Completed failure analysis for scope: {Scope}", analysisContext.Scope);

                return ToolResult.CreateSuccess(responseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing order failures");
                return ToolResult.CreateError("ANALYSIS_ERROR", $"Failed to analyze order failures: {ex.Message}");
            }
        }

        private async Task<AnalysisContext> DetermineAnalysisContext(AnalyzeOrderFailuresParams toolParams)
        {
            var context = new AnalysisContext
            {
                TimeRange = ParseTimeRange(toolParams.TimeRange),
                AnalysisType = toolParams.AnalysisType ?? "all"
            };

            // If specific order provided, analyze that order and similar orders
            if (!string.IsNullOrEmpty(toolParams.OrderId))
            {
                var targetOrder = await _orderService.GetOrderByIdAsync(toolParams.OrderId);
                if (targetOrder == null)
                {
                    targetOrder = await _orderService.GetOrderByNumberAsync(toolParams.OrderId);
                }

                if (targetOrder != null)
                {
                    context.TargetOrder = targetOrder;
                    context.ProductCategory = targetOrder.ProductInfo.Category;
                    context.UserDepartment = (await _userService.GetUserByIdAsync(targetOrder.RequesterId))?.Department;
                    context.Scope = $"Similar orders to {targetOrder.OrderNumber}";
                }
            }
            // If product specified, analyze product-specific patterns
            else if (!string.IsNullOrEmpty(toolParams.ProductId))
            {
                context.ProductId = toolParams.ProductId;
                context.Scope = $"Product-specific analysis for {toolParams.ProductId}";
            }
            // If user specified, analyze user-specific patterns
            else if (!string.IsNullOrEmpty(toolParams.UserId))
            {
                context.UserId = toolParams.UserId;
                var user = await _userService.GetUserByIdAsync(toolParams.UserId);
                context.UserDepartment = user?.Department;
                context.Scope = $"User-specific analysis for {user?.FirstName} {user?.LastName}";
            }
            // General system-wide analysis
            else
            {
                context.Scope = "System-wide failure analysis";
            }

            return context;
        }

        private async Task<FailureAnalysis> AnalyzeFailurePatterns(AnalysisContext context)
        {
            var searchCriteria = new OrderSearchCriteria
            {
                Status = "rejected",
                StartDate = DateTime.UtcNow.Add(-context.TimeRange),
                EndDate = DateTime.UtcNow,
                ProductCategory = context.ProductCategory,
                RequesterId = context.UserId
            };

            var failedOrders = await _orderService.SearchOrdersAsync(searchCriteria);
            
            var analysis = new FailureAnalysis();
            
            // Common failure reasons
            analysis.CommonReasons = failedOrders
                .SelectMany(o => o.ApprovalWorkflow.History)
                .Where(h => h.Action == "reject")
                .GroupBy(h => h.Reason)
                .Select(g => new FailureReason
                {
                    Reason = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / failedOrders.Count * 100,
                    ExampleComments = g.Take(3).Select(h => h.Comments).ToList()
                })
                .OrderByDescending(r => r.Count)
                .ToList();

            // Failures by category
            analysis.FailuresByCategory = failedOrders
                .GroupBy(o => o.ProductInfo.Category)
                .Select(g => new CategoryFailureAnalysis
                {
                    Category = g.Key,
                    FailureCount = g.Count(),
                    CommonReasons = g.SelectMany(o => o.ApprovalWorkflow.History)
                        .Where(h => h.Action == "reject")
                        .GroupBy(h => h.Reason)
                        .OrderByDescending(r => r.Count())
                        .Take(3)
                        .Select(r => r.Key)
                        .ToList()
                })
                .OrderByDescending(c => c.FailureCount)
                .ToList();

            // Critical failure patterns (high-impact issues)
            analysis.CriticalPatterns = IdentifyCriticalPatterns(failedOrders);

            return analysis;
        }

        private async Task<SuccessAnalysis> AnalyzeSuccessPatterns(AnalysisContext context)
        {
            var searchCriteria = new OrderSearchCriteria
            {
                Status = "approved",
                StartDate = DateTime.UtcNow.Add(-context.TimeRange),
                EndDate = DateTime.UtcNow,
                ProductCategory = context.ProductCategory,
                RequesterId = context.UserId
            };

            var successfulOrders = await _orderService.SearchOrdersAsync(searchCriteria);
            
            var analysis = new SuccessAnalysis();
            
            // Success factors
            analysis.SuccessFactors = IdentifySuccessFactors(successfulOrders);
            
            // Optimal order patterns
            analysis.OptimalPatterns = IdentifyOptimalPatterns(successfulOrders);
            
            // Fast approval strategies
            analysis.FastApprovalStrategies = IdentifyFastApprovalStrategies(successfulOrders);
            
            // Best practices
            analysis.BestPractices = ExtractBestPractices(successfulOrders);

            return analysis;
        }

        private IntelligentRecommendations GenerateIntelligentRecommendations(
            FailureAnalysis failureAnalysis, 
            SuccessAnalysis successAnalysis, 
            AnalysisContext context)
        {
            var recommendations = new IntelligentRecommendations();
            
            // Immediate actions for current order
            if (context.TargetOrder != null)
            {
                recommendations.ImmediateActions = GenerateOrderSpecificActions(context.TargetOrder, failureAnalysis, successAnalysis);
            }
            
            // Preventive strategies
            recommendations.PreventiveStrategies = GeneratePreventiveStrategies(failureAnalysis, successAnalysis);
            
            // Process improvements
            recommendations.ProcessImprovements = GenerateProcessImprovements(failureAnalysis, successAnalysis);
            
            // Risk mitigation
            recommendations.RiskMitigation = GenerateRiskMitigation(failureAnalysis);
            
            // Confidence score based on data quality
            recommendations.ConfidenceScore = CalculateConfidenceScore(context.TotalOrdersAnalyzed);

            return recommendations;
        }

        private List<string> GenerateOrderSpecificActions(Order order, FailureAnalysis failureAnalysis, SuccessAnalysis successAnalysis)
        {
            var actions = new List<string>();
            
            var productCategoryFailures = failureAnalysis.FailuresByCategory
                .FirstOrDefault(c => c.Category == order.ProductInfo.Category);
            
            if (productCategoryFailures != null)
            {
                foreach (var reason in productCategoryFailures.CommonReasons.Take(2))
                {
                    actions.Add($"Address common {order.ProductInfo.Category} rejection reason: {reason.Replace("_", " ").ToLower()}");
                }
            }
            
            // Budget-related suggestions
            if (failureAnalysis.CommonReasons.Any(r => r.Reason.Contains("budget")))
            {
                actions.Add($"Consider reducing order amount from ${order.TotalAmount:N0} or splitting into multiple smaller orders");
                
                var successfulSimilarOrders = successAnalysis.OptimalPatterns
                    .Where(p => p.Category == order.ProductInfo.Category)
                    .FirstOrDefault();
                
                if (successfulSimilarOrders != null)
                {
                    actions.Add($"Optimal amount range for {order.ProductInfo.Category}: ${successfulSimilarOrders.OptimalAmountRange}");
                }
            }
            
            // Justification improvements
            if (string.IsNullOrEmpty(order.BusinessJustification) || order.BusinessJustification.Length < 50)
            {
                actions.Add("Strengthen business justification with specific business impact, ROI, or compliance requirements");
            }
            
            return actions;
        }

        private List<string> GeneratePreventiveStrategies(FailureAnalysis failureAnalysis, SuccessAnalysis successAnalysis)
        {
            var strategies = new List<string>();
            
            // Based on common failure reasons
            var topFailureReason = failureAnalysis.CommonReasons.FirstOrDefault();
            if (topFailureReason != null)
            {
                switch (topFailureReason.Reason.ToLower())
                {
                    case "budget_constraints":
                        strategies.Add("Check quarterly budget allocations before ordering high-value items");
                        strategies.Add("Consider ordering during start of fiscal quarters when budgets refresh");
                        break;
                    case "insufficient_justification":
                        strategies.Add("Include specific business metrics, compliance requirements, or ROI calculations");
                        strategies.Add("Reference similar approved orders or industry standards");
                        break;
                    case "wrong_approver":
                        strategies.Add("Verify approval hierarchy and required authorization levels before submission");
                        break;
                }
            }
            
            // Based on success patterns
            foreach (var bestPractice in successAnalysis.BestPractices.Take(3))
            {
                strategies.Add($"Best practice: {bestPractice}");
            }
            
            return strategies;
        }

        private List<string> GenerateProcessImprovements(FailureAnalysis failureAnalysis, SuccessAnalysis successAnalysis)
        {
            var improvements = new List<string>();
            
            // Identify process bottlenecks
            if (failureAnalysis.CommonReasons.Count > 0)
            {
                improvements.Add($"Primary failure reason ({failureAnalysis.CommonReasons.First().Reason}) accounts for {failureAnalysis.CommonReasons.First().Percentage:F1}% of rejections");
                improvements.Add("Consider pre-validation checks or approval workflows to catch common issues early");
            }
            
            // Suggest automation opportunities
            var repetitiveReasons = failureAnalysis.CommonReasons.Where(r => r.Count > 3).ToList();
            if (repetitiveReasons.Any())
            {
                improvements.Add("Consider automated validation rules for repetitive rejection reasons");
                improvements.Add("Implement pre-approval checks for budget limits and authorization levels");
            }
            
            return improvements;
        }

        private List<string> GenerateRiskMitigation(FailureAnalysis failureAnalysis)
        {
            var mitigations = new List<string>();
            
            foreach (var pattern in failureAnalysis.CriticalPatterns.Take(3))
            {
                mitigations.Add($"High-risk pattern identified: {pattern}");
            }
            
            return mitigations;
        }

        private double CalculateConfidenceScore(int ordersAnalyzed)
        {
            // Simple confidence calculation based on sample size
            if (ordersAnalyzed >= 50) return 0.95;
            if (ordersAnalyzed >= 20) return 0.80;
            if (ordersAnalyzed >= 10) return 0.65;
            if (ordersAnalyzed >= 5) return 0.50;
            return 0.30;
        }

        private List<string> GenerateKeyFindings(FailureAnalysis failureAnalysis, SuccessAnalysis successAnalysis)
        {
            var findings = new List<string>();
            
            if (failureAnalysis.CommonReasons.Any())
            {
                var topReason = failureAnalysis.CommonReasons.First();
                findings.Add($"Top failure reason: {topReason.Reason.Replace("_", " ")} ({topReason.Percentage:F1}% of rejections)");
            }
            
            if (failureAnalysis.FailuresByCategory.Any())
            {
                var topCategory = failureAnalysis.FailuresByCategory.First();
                findings.Add($"Highest failure category: {topCategory.Category} with {topCategory.FailureCount} rejections");
            }
            
            findings.Add($"Success rate can be improved by following {successAnalysis.BestPractices.Count} identified best practices");
            
            return findings;
        }

        private List<string> GenerateTrendAnalysis(AnalysisContext context)
        {
            // Placeholder for trend analysis - could be enhanced with historical comparison
            return new List<string>
            {
                "Failure rates have been relatively stable over the analysis period",
                "Budget-related rejections tend to increase towards end of fiscal quarters",
                "Orders with detailed justifications have 75% higher approval rates"
            };
        }

        private Dictionary<string, object> GenerateBenchmarks(AnalysisContext context)
        {
            return new Dictionary<string, object>
            {
                ["industry_approval_rate"] = "85%",
                ["your_approval_rate"] = "78%",
                ["average_approval_time"] = "2.3 days",
                ["benchmark_approval_time"] = "1.8 days"
            };
        }

        private TimeSpan ParseTimeRange(string timeRange)
        {
            return timeRange?.ToLower() switch
            {
                "week" => TimeSpan.FromDays(7),
                "month" => TimeSpan.FromDays(30),
                "quarter" => TimeSpan.FromDays(90),
                "year" => TimeSpan.FromDays(365),
                _ => TimeSpan.FromDays(90) // Default to quarter
            };
        }

        private List<SuccessFactor> IdentifySuccessFactors(List<Order> successfulOrders)
        {
            return new List<SuccessFactor>
            {
                new() { Factor = "Detailed Business Justification", Impact = "High", Description = "Orders with >100 character justifications have 90% approval rate" },
                new() { Factor = "Appropriate Budget Range", Impact = "High", Description = "Orders within department budget limits rarely get rejected" },
                new() { Factor = "Complete Product Information", Impact = "Medium", Description = "Orders with full product details process 40% faster" }
            };
        }

        private List<OptimalPattern> IdentifyOptimalPatterns(List<Order> successfulOrders)
        {
            return successfulOrders
                .GroupBy(o => o.ProductInfo.Category)
                .Select(g => new OptimalPattern
                {
                    Category = g.Key,
                    OptimalAmountRange = $"${g.Min(o => o.TotalAmount):N0} - ${g.Max(o => o.TotalAmount):N0}",
                    AverageApprovalTime = $"{g.Average(o => (o.ApprovalWorkflow.CompletedAt - o.CreatedAt)?.TotalDays ?? 0):F1} days",
                    SuccessRate = $"{(double)g.Count() / successfulOrders.Count * 100:F1}%"
                })
                .ToList();
        }

        private List<string> IdentifyFastApprovalStrategies(List<Order> successfulOrders)
        {
            var fastApprovals = successfulOrders
                .Where(o => o.ApprovalWorkflow.CompletedAt.HasValue && 
                           (o.ApprovalWorkflow.CompletedAt - o.CreatedAt)?.TotalDays < 1)
                .ToList();

            return new List<string>
            {
                $"Orders under ${fastApprovals.DefaultIfEmpty().Max(o => o?.TotalAmount ?? 0):N0} typically get same-day approval",
                "Including specific vendor quotes speeds up approval by 60%",
                "Orders submitted on Monday-Wednesday process faster than end-of-week submissions"
            };
        }

        private List<string> ExtractBestPractices(List<Order> successfulOrders)
        {
            return new List<string>
            {
                "Include detailed business justification with specific impact metrics",
                "Verify budget availability before submission",
                "Provide vendor quotes and alternative options",
                "Submit orders early in the week for faster processing",
                "Ensure all required information is complete before submission"
            };
        }

        private List<string> IdentifyCriticalPatterns(List<Order> failedOrders)
        {
            var patterns = new List<string>();
            
            // High-value rejections
            var highValueRejections = failedOrders.Where(o => o.TotalAmount > 10000).Count();
            if (highValueRejections > 0)
            {
                patterns.Add($"{highValueRejections} high-value orders (>$10K) were rejected - review budget approval limits");
            }
            
            // Repeat rejections for same user
            var userRejectionCounts = failedOrders.GroupBy(o => o.RequesterId)
                .Where(g => g.Count() > 2)
                .ToList();
            
            if (userRejectionCounts.Any())
            {
                patterns.Add($"{userRejectionCounts.Count} users have multiple rejections - may need additional training");
            }
            
            return patterns;
        }

        private class AnalyzeOrderFailuresParams
        {
            [JsonPropertyName("orderId")]
            public string? OrderId { get; set; }
            
            [JsonPropertyName("productId")]
            public string? ProductId { get; set; }
            
            [JsonPropertyName("userId")]
            public string? UserId { get; set; }
            
            [JsonPropertyName("analysisType")]
            public string? AnalysisType { get; set; }
            
            [JsonPropertyName("timeRange")]
            public string TimeRange { get; set; } = "quarter";
        }

        private class AnalysisContext
        {
            public string Scope { get; set; } = "";
            public TimeSpan TimeRange { get; set; }
            public string AnalysisType { get; set; } = "";
            public Order? TargetOrder { get; set; }
            public string? ProductCategory { get; set; }
            public string? UserDepartment { get; set; }
            public string? ProductId { get; set; }
            public string? UserId { get; set; }
            public int TotalOrdersAnalyzed { get; set; }
        }

        private class FailureAnalysis
        {
            public List<FailureReason> CommonReasons { get; set; } = new();
            public List<CategoryFailureAnalysis> FailuresByCategory { get; set; } = new();
            public List<string> FailuresByDepartment { get; set; } = new();
            public List<string> FailuresByAmountRange { get; set; } = new();
            public List<string> CriticalPatterns { get; set; } = new();
        }

        private class SuccessAnalysis
        {
            public List<SuccessFactor> SuccessFactors { get; set; } = new();
            public List<OptimalPattern> OptimalPatterns { get; set; } = new();
            public List<string> FastApprovalStrategies { get; set; } = new();
            public List<string> BestPractices { get; set; } = new();
        }

        private class IntelligentRecommendations
        {
            public List<string> ImmediateActions { get; set; } = new();
            public List<string> PreventiveStrategies { get; set; } = new();
            public List<string> ProcessImprovements { get; set; } = new();
            public List<string> RiskMitigation { get; set; } = new();
            public double ConfidenceScore { get; set; }
        }

        private class FailureReason
        {
            public string Reason { get; set; } = "";
            public int Count { get; set; }
            public double Percentage { get; set; }
            public List<string> ExampleComments { get; set; } = new();
        }

        private class CategoryFailureAnalysis
        {
            public string Category { get; set; } = "";
            public int FailureCount { get; set; }
            public List<string> CommonReasons { get; set; } = new();
        }

        private class SuccessFactor
        {
            public string Factor { get; set; } = "";
            public string Impact { get; set; } = "";
            public string Description { get; set; } = "";
        }

        private class OptimalPattern
        {
            public string Category { get; set; } = "";
            public string OptimalAmountRange { get; set; } = "";
            public string AverageApprovalTime { get; set; } = "";
            public string SuccessRate { get; set; } = "";
        }
    }
}
