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
    /// Tool for retrieving user orders with advanced filtering
    /// </summary>
    public class GetUserOrdersTool : BaseMCPTool, IAgentTool
    {
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;

        public GetUserOrdersTool(IOrderService orderService, IUserService userService, ILogger<GetUserOrdersTool> logger)
            : base(logger)
        {
            _orderService = orderService;
            _userService = userService;
        }

        public override string Name => "get_user_orders";

        public override string Description => "Retrieve user orders with advanced filtering capabilities including status, date range, and product category filters";

        public override ParameterSchema Parameters => new()
        {
            Type = "object",
            Properties = new Dictionary<string, ParameterProperty>
            {
                ["userId"] = new()
                {
                    Type = "string",
                    Description = "User ID to retrieve orders for",
                    Required = true,
                    MinLength = 1
                },
                ["status"] = new()
                {
                    Type = "array",
                    Description = "Array of order statuses to filter by",
                    Required = false
                },
                ["dateRange"] = new()
                {
                    Type = "object",
                    Description = "Date range filter with start and end dates",
                    Required = false
                },
                ["productCategory"] = new()
                {
                    Type = "string",
                    Description = "Product category to filter by",
                    Required = false
                },
                ["limit"] = new()
                {
                    Type = "integer",
                    Description = "Maximum number of orders to return",
                    Required = false,
                    Minimum = 1,
                    Maximum = 100,
                    Default = 20
                },
                ["offset"] = new()
                {
                    Type = "integer",
                    Description = "Number of orders to skip for pagination",
                    Required = false,
                    Minimum = 0,
                    Default = 0
                },
                ["includeHistory"] = new()
                {
                    Type = "boolean",
                    Description = "Whether to include approval history details",
                    Required = false,
                    Default = false
                }
            },
            Required = new List<string> { "userId" }
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
                    PropertyNameCaseInsensitive = true,
                    Converters = { new FlexibleStringListConverter() }
                };
                var toolParams = JsonSerializer.Deserialize<GetUserOrdersParams>(json, options);

                if (toolParams == null || string.IsNullOrEmpty(toolParams.UserId))
                {
                    return ToolResult.CreateError("INVALID_PARAMETERS", "UserId is required");
                }

                if (toolParams.UserId != context?.UserId)
                {
                    return ToolResult.CreateError("UNAUTHORIZED", "You can only access your own orders");
                }

                // Verify user exists
                var user = await _userService.GetUserByIdAsync(toolParams.UserId);
                if (user == null)
                {
                    return ToolResult.CreateError("USER_NOT_FOUND", $"User with ID '{toolParams.UserId}' not found");
                }

                // Get orders with filtering
                var orders = await _orderService.GetOrdersByUserAsync(toolParams.UserId);

                // Apply status filter
                if (toolParams.Status != null && toolParams.Status.Any())
                {
                    orders = orders.Where(o => toolParams.Status.Contains(o.Status)).ToList();
                }

                // Apply date range filter
                if (toolParams.DateRange != null)
                {
                    if (toolParams.DateRange.Start.HasValue)
                    {
                        orders = orders.Where(o => o.CreatedAt >= toolParams.DateRange.Start.Value).ToList();
                    }
                    if (toolParams.DateRange.End.HasValue)
                    {
                        orders = orders.Where(o => o.CreatedAt <= toolParams.DateRange.End.Value).ToList();
                    }
                }

                // Apply product category filter
                if (!string.IsNullOrEmpty(toolParams.ProductCategory))
                {
                    // Filter by product category
                    orders = orders.Where(o => o.ProductInfo.Category?.Contains(toolParams.ProductCategory, StringComparison.OrdinalIgnoreCase) == true).ToList();
                }

                // Get total count before pagination
                var totalCount = orders.Count;

                // Apply pagination
                var offset = toolParams.Offset ?? 0;
                var limit = toolParams.Limit ?? 20;
                var paginatedOrders = orders.Skip(offset).Take(limit).ToList();

                // Prepare response data
                var responseData = new
                {
                    orders = paginatedOrders.Select(order => new
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
                        approvalWorkflow = order.ApprovalWorkflow,
                        approvalHistory = toolParams.IncludeHistory == true ? order.ApprovalWorkflow.History : null,
                        totalAmount = order.TotalAmount,
                        currency = order.Currency
                    }),
                    totalCount,
                    pagination = new
                    {
                        offset,
                        limit,
                        totalPages = (int)Math.Ceiling((double)totalCount / limit),
                        hasNext = offset + limit < totalCount,
                        hasPrevious = offset > 0
                    },
                    summary = new
                    {
                        totalOrders = totalCount,
                        statusBreakdown = orders.GroupBy(o => o.Status)
                            .ToDictionary(g => g.Key, g => g.Count()),
                        recentActivity = orders
                            .Where(o => o.UpdatedAt >= DateTime.UtcNow.AddDays(-7))
                            .Count()
                    }
                };

                _logger.LogInformation("Retrieved {Count} orders for user {UserId} with filters", paginatedOrders.Count, toolParams.UserId);

                return ToolResult.CreateSuccess(responseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for user");
                return ToolResult.CreateError("RETRIEVAL_ERROR", $"Failed to retrieve user orders: {ex.Message}");
            }
        }

        private sealed class GetUserOrdersParams
        {
            [JsonPropertyName("userId")]
            public string UserId { get; set; } = string.Empty;
            
            [JsonPropertyName("status")]
            public List<string>? Status { get; set; }
            
            [JsonPropertyName("dateRange")]
            public DateRangeFilter? DateRange { get; set; }
            
            [JsonPropertyName("productCategory")]
            public string? ProductCategory { get; set; }
            
            [JsonPropertyName("limit")]
            public int? Limit { get; set; }
            
            [JsonPropertyName("offset")]
            public int? Offset { get; set; }
            
            [JsonPropertyName("includeHistory")]
            public bool? IncludeHistory { get; set; }
        }

        private sealed class DateRangeFilter
        {
            public DateTime? Start { get; set; }
            public DateTime? End { get; set; }
        }

        /// <summary>
        /// Custom JSON converter that handles both string and List<string> for the status parameter
        /// </summary>
        private sealed class FlexibleStringListConverter : JsonConverter<List<string>?>
        {
            public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return null;
                
                if (reader.TokenType == JsonTokenType.String)
                {
                    var singleValue = reader.GetString();
                    return string.IsNullOrEmpty(singleValue) ? null : new List<string> { singleValue };
                }
                
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    var list = new List<string>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            var value = reader.GetString();
                            if (!string.IsNullOrEmpty(value))
                                list.Add(value);
                        }
                    }
                    return list.Any() ? list : null;
                }
                
                return null;
            }

            public override void Write(Utf8JsonWriter writer, List<string>? value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteStartArray();
                    foreach (var item in value)
                    {
                        writer.WriteStringValue(item);
                    }
                    writer.WriteEndArray();
                }
            }
        }

        // Implement IAgentTool interface
        string IAgentTool.Name => "get_user_orders";
        string IAgentTool.Description => "Retrieve user orders with advanced filtering capabilities including status, date range, and product category filters";

        async Task<AgentToolResult> IAgentTool.ExecuteAsync(AgentToolContext context)
        {
            try
            {
                var parameters = new Dictionary<string, object>();
                
                if (context.Parameters.ContainsKey("userId"))
                    parameters["userId"] = context.Parameters["userId"];
                if (context.Parameters.ContainsKey("status"))
                    parameters["status"] = context.Parameters["status"];
                if (context.Parameters.ContainsKey("dateRange"))
                    parameters["dateRange"] = context.Parameters["dateRange"];
                if (context.Parameters.ContainsKey("productCategory"))
                    parameters["productCategory"] = context.Parameters["productCategory"];
                if (context.Parameters.ContainsKey("limit"))
                    parameters["limit"] = context.Parameters["limit"];
                if (context.Parameters.ContainsKey("offset"))
                    parameters["offset"] = context.Parameters["offset"];
                if (context.Parameters.ContainsKey("includeHistory"))
                    parameters["includeHistory"] = context.Parameters["includeHistory"];

                var mcpResult = await ExecuteInternalAsync(parameters, CancellationToken.None, context);

                if (!mcpResult.Success)
                {
                    return new AgentToolResult
                    {
                        Success = false,
                        Error = mcpResult.Error?.Message ?? "Unknown error",
                        Output = "Failed to retrieve user orders"
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
                    Output = "User orders retrieved successfully",
                    Data = deserializedData ?? new Dictionary<string, object>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetUserOrdersTool in agent context");
                return new AgentToolResult
                {
                    Success = false,
                    Error = ex.Message,
                    Output = "Failed to retrieve user orders"
                };
            }
        }
    }
}
