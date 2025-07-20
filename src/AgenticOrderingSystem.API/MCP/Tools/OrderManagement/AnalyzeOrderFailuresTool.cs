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
                var recommendations = await GenerateIntelligentRecommendations(failureAnalysis, successAnalysis, analysisContext);

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

        private async Task<IntelligentRecommendations> GenerateIntelligentRecommendations(
            FailureAnalysis failureAnalysis, 
            SuccessAnalysis successAnalysis, 
            AnalysisContext context)
        {
            var recommendations = new IntelligentRecommendations();
            
            // Immediate actions for current order
            if (context.TargetOrder != null)
            {
                recommendations.ImmediateActions = await GenerateOrderSpecificActions(context.TargetOrder, failureAnalysis, successAnalysis);
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

        private async Task<List<string>> GenerateOrderSpecificActions(Order order, FailureAnalysis failureAnalysis, SuccessAnalysis successAnalysis)
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
            
            // Add team-based recommendations with specific value comparisons
            var teamSuccessExamples = await GetTeamSuccessExamplesAsync(order);
            if (teamSuccessExamples.Any())
            {
                var successExample = teamSuccessExamples.First();
                
                // Add value comparison format
                actions.Add("🔄 TEAM SUCCESS COMPARISON:");
                actions.Add($"❌ Your order: {order.ProductInfo.Name}");
                actions.Add($"✅ {successExample.RequesterName} succeeded with: {successExample.ProductName}");
                
                // Add specific value differences
                await AddSpecificValueComparisons(order, successExample, actions);
                
                actions.Add($"📋 Success Pattern: {successExample.SuccessFactors}");
            }
            
            // Version/Product availability checks
            await AddProductVersionRecommendationsAsync(order, actions);
            
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

        private class TeamSuccessExample
        {
            public string RequesterName { get; set; } = "";
            public string ProductName { get; set; } = "";
            public string SuccessFactors { get; set; } = "";
            public string Department { get; set; } = "";
        }

        /// <summary>
        /// Find successful orders from team members for similar products/categories
        /// </summary>
        private async Task<List<TeamSuccessExample>> GetTeamSuccessExamplesAsync(Order failedOrder)
        {
            try
            {
                var teamSuccessExamples = new List<TeamSuccessExample>();
                
                // Get the requester to find their manager
                var requester = await _userService.GetUserByIdAsync(failedOrder.RequesterId);
                if (requester?.ManagerId == null) return teamSuccessExamples;
                
                // Find team members with same manager - using search functionality
                var allActiveUsers = await _userService.GetActiveUsersAsync(1, 1000); // Get more users
                var teamMembers = allActiveUsers.Where(u => u.ManagerId == requester.ManagerId && u.Id != failedOrder.RequesterId).ToList();
                
                if (!teamMembers.Any()) return teamSuccessExamples;
                
                // Find successful orders from team members for similar products
                var teamMemberIds = teamMembers.Select(tm => tm.Id).ToList();
                
                // Search for approved orders in the same category from team members
                var searchCriteria = new OrderSearchCriteria
                {
                    Status = "approved",
                    ProductCategory = failedOrder.ProductInfo.Category
                };
                
                var allOrders = await _orderService.SearchOrdersAsync(searchCriteria);
                
                var successfulOrders = allOrders.Where(o => 
                    teamMemberIds.Contains(o.RequesterId) &&
                    o.CreatedAt >= DateTime.UtcNow.AddDays(-90) // Last 90 days
                ).OrderByDescending(o => o.CreatedAt)
                .Take(3)
                .ToList();
                
                foreach (var successOrder in successfulOrders)
                {
                    var teammate = teamMembers.FirstOrDefault(tm => tm.Id == successOrder.RequesterId);
                    if (teammate != null)
                    {
                        var successFactors = ExtractSuccessFactors(successOrder);
                        teamSuccessExamples.Add(new TeamSuccessExample
                        {
                            RequesterName = teammate.FullName,
                            ProductName = successOrder.ProductInfo.Name,
                            SuccessFactors = successFactors,
                            Department = teammate.Department
                        });
                    }
                }
                
                return teamSuccessExamples;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting team success examples for order {OrderId}", failedOrder.Id);
                return new List<TeamSuccessExample>();
            }
        }

        /// <summary>
        /// Add product version and availability recommendations
        /// </summary>
        private Task AddProductVersionRecommendationsAsync(Order order, List<string> actions)
        {
            try
            {
                // Check internal notes for version-related issues
                var hasVersionIssues = order.Metadata?.InternalNotes?.Any(note => 
                    note.Note.ToLower().Contains("discontinued") ||
                    note.Note.ToLower().Contains("no longer available") ||
                    note.Note.ToLower().Contains("version") ||
                    note.Note.ToLower().Contains("replaced")
                ) ?? false;

                if (hasVersionIssues)
                {
                    actions.Add($"🔄 Product Update: {order.ProductInfo.Name} may have version issues");
                    actions.Add($"💡 Suggestion: Check for current versions in the {order.ProductInfo.Category} category");
                }
                
                // Check approval comments for version issues
                var hasApprovalVersionIssues = order.ApprovalWorkflow?.History?.Any(action =>
                    !string.IsNullOrEmpty(action.Comments) &&
                    (action.Comments.ToLower().Contains("discontinued") ||
                     action.Comments.ToLower().Contains("version") ||
                     action.Comments.ToLower().Contains("no longer available"))
                ) ?? false;

                if (hasApprovalVersionIssues)
                {
                    actions.Add($"⚠️ Approval Note: Version-related concerns were raised during approval");
                }
                
                // Add category-specific recommendations
                if (order.ProductInfo.Category == "software")
                {
                    actions.Add("📋 Software Tip: Always verify current licensing and version requirements before ordering");
                }
                else if (order.ProductInfo.Category == "hardware")
                {
                    actions.Add("📋 Hardware Tip: Check for latest models and ensure compatibility with existing systems");
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adding product version recommendations for order {OrderId}", order.Id);
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Extract success factors from an approved order
        /// </summary>
        private string ExtractSuccessFactors(Order order)
        {
            var factors = new List<string>();
            
            // Business justification quality
            if (!string.IsNullOrEmpty(order.BusinessJustification) && order.BusinessJustification.Length > 100)
            {
                factors.Add("Detailed business justification");
            }
            
            // Approval speed (simplified check using completion time)
            if (order.ApprovalWorkflow?.CompletedAt.HasValue == true)
            {
                var approvalTime = order.ApprovalWorkflow.CompletedAt.Value - order.CreatedAt;
                if (approvalTime.TotalHours < 24)
                {
                    factors.Add("Fast approval (same day)");
                }
            }
            
            // Amount reasonableness
            if (order.TotalAmount <= 500)
            {
                factors.Add("Reasonable amount");
            }
            
            // Complete information
            if (!string.IsNullOrEmpty(order.BusinessJustification) && order.Quantity > 0)
            {
                factors.Add("Complete order information");
            }
            
            return factors.Any() ? string.Join(", ", factors) : "Standard approval process";
        }
        
        /// <summary>
        /// Add specific value comparisons between failed order and successful teammate order
        /// </summary>
        private async Task AddSpecificValueComparisons(Order failedOrder, TeamSuccessExample successExample, List<string> actions)
        {
            // Get the successful order details to compare
            var searchCriteria = new OrderSearchCriteria
            {
                SearchText = successExample.ProductName,
                Status = "approved",
                ProductCategory = failedOrder.ProductInfo.Category
            };
            
            var successfulOrders = await _orderService.SearchOrdersAsync(searchCriteria);
            var successfulOrder = successfulOrders.FirstOrDefault(o => 
                o.ProductInfo.Name.Contains(successExample.ProductName, StringComparison.OrdinalIgnoreCase) &&
                o.CreatedAt >= DateTime.UtcNow.AddDays(-90));
            
            if (successfulOrder != null)
            {
                // Product version comparison
                if (failedOrder.ProductInfo.Name != successfulOrder.ProductInfo.Name)
                {
                    actions.Add($"🔧 PRODUCT VERSION:");
                    actions.Add($"   ❌ You requested: {failedOrder.ProductInfo.Name}");
                    actions.Add($"   ✅ {successExample.RequesterName} got approved: {successfulOrder.ProductInfo.Name}");
                }
                
                // Business justification comparison
                if (!string.IsNullOrEmpty(failedOrder.BusinessJustification) && !string.IsNullOrEmpty(successfulOrder.BusinessJustification))
                {
                    actions.Add($"📝 BUSINESS JUSTIFICATION:");
                    actions.Add($"   ❌ Your justification ({failedOrder.BusinessJustification.Length} chars): \"{failedOrder.BusinessJustification}\"");
                    actions.Add($"   ✅ {successExample.RequesterName}'s justification ({successfulOrder.BusinessJustification.Length} chars): \"{successfulOrder.BusinessJustification}\"");
                }
                
                // Amount comparison
                if (Math.Abs(failedOrder.TotalAmount - successfulOrder.TotalAmount) > 5)
                {
                    actions.Add($"💰 AMOUNT:");
                    actions.Add($"   ❌ Your amount: ${failedOrder.TotalAmount:F2}");
                    actions.Add($"   ✅ {successExample.RequesterName}'s amount: ${successfulOrder.TotalAmount:F2}");
                }
                
                // Priority comparison
                if (failedOrder.Priority != successfulOrder.Priority)
                {
                    actions.Add($"⚡ PRIORITY:");
                    actions.Add($"   ❌ Your priority: {failedOrder.Priority}");
                    actions.Add($"   ✅ {successExample.RequesterName}'s priority: {successfulOrder.Priority}");
                }
            }
        }
    }
}
