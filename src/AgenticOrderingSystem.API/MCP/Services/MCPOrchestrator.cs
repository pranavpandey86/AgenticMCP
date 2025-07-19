using AgenticOrderingSystem.API.MCP.Interfaces;
using AgenticOrderingSystem.API.MCP.Models;
using AgenticOrderingSystem.API.MCP.Tools.OrderManagement;
using System.Diagnostics;

namespace AgenticOrderingSystem.API.MCP.Services
{
    /// <summary>
    /// Service for orchestrating MCP tools and managing tool execution
    /// </summary>
    public class MCPOrchestrator : IMCPOrchestrator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MCPOrchestrator> _logger;
        private readonly Dictionary<string, Type> _toolTypes;

        public MCPOrchestrator(IServiceProvider serviceProvider, ILogger<MCPOrchestrator> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _toolTypes = new Dictionary<string, Type>();
            
            RegisterTools();
        }

        public IEnumerable<IMCPTool> GetAvailableTools()
        {
            var tools = new List<IMCPTool>();

            _logger.LogInformation("Attempting to get {ToolCount} registered tools", _toolTypes.Count);
            _logger.LogInformation("Registered tool types: {ToolTypes}", string.Join(", ", _toolTypes.Keys));

            foreach (var kvp in _toolTypes)
            {
                var toolName = kvp.Key;
                var toolType = kvp.Value;
                
                try
                {
                    _logger.LogInformation("Attempting to instantiate tool {ToolName} with type {ToolType}", toolName, toolType.Name);
                    
                    // First get the service
                    var service = _serviceProvider.GetRequiredService(toolType);
                    _logger.LogInformation("Successfully got service instance of type {ActualType}", service.GetType().Name);
                    
                    // Then cast to IMCPTool
                    if (service is IMCPTool tool)
                    {
                        tools.Add(tool);
                        _logger.LogInformation("Successfully cast and added tool {ToolName}", tool.Name);
                    }
                    else
                    {
                        _logger.LogError("Service of type {ServiceType} is not an IMCPTool", service.GetType().Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to instantiate tool {ToolName} with type {ToolType}. Exception: {ExceptionType}", 
                        toolName, toolType.Name, ex.GetType().Name);
                }
            }

            _logger.LogInformation("Successfully instantiated {SuccessCount} out of {TotalCount} tools", tools.Count, _toolTypes.Count);

            return tools;
        }

        public IMCPTool? GetTool(string toolName)
        {
            _logger.LogInformation("Getting tool {ToolName}", toolName);
            
            if (!_toolTypes.TryGetValue(toolName, out var toolType))
            {
                _logger.LogWarning("Tool {ToolName} not found in registered types. Available tools: {AvailableTools}", 
                    toolName, string.Join(", ", _toolTypes.Keys));
                return null;
            }

            _logger.LogInformation("Found tool type {ToolType} for {ToolName}", toolType.Name, toolName);

            try
            {
                var service = _serviceProvider.GetRequiredService(toolType);
                _logger.LogInformation("Successfully resolved service of type {ActualType}", service.GetType().Name);
                
                if (service is IMCPTool tool)
                {
                    _logger.LogInformation("Successfully cast to IMCPTool with name {ToolName}", tool.Name);
                    return tool;
                }
                else
                {
                    _logger.LogError("Service of type {ServiceType} is not an IMCPTool", service.GetType().Name);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to instantiate tool {ToolName} with type {ToolType}. Exception: {ExceptionType}", 
                    toolName, toolType.Name, ex.GetType().Name);
                return null;
            }
        }

        public async Task<ToolResult> ExecuteToolAsync(string toolName, object parameters, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Executing tool {ToolName}", toolName);

                var tool = GetTool(toolName);
                if (tool == null)
                {
                    return ToolResult.CreateError("TOOL_NOT_FOUND", $"Tool '{toolName}' not found or could not be instantiated");
                }

                var result = await tool.ExecuteAsync(parameters, cancellationToken);
                
                _logger.LogInformation("Tool {ToolName} executed in {ElapsedMs}ms with success: {Success}", 
                    toolName, stopwatch.ElapsedMilliseconds, result.Success);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Tool {ToolName} execution was cancelled after {ElapsedMs}ms", toolName, stopwatch.ElapsedMilliseconds);
                return ToolResult.CreateError("OPERATION_CANCELLED", $"Tool '{toolName}' execution was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error executing tool {ToolName} after {ElapsedMs}ms", toolName, stopwatch.ElapsedMilliseconds);
                return ToolResult.CreateError("ORCHESTRATOR_ERROR", $"Unexpected error executing tool '{toolName}': {ex.Message}");
            }
        }

        public async Task<List<ToolResult>> ExecuteToolsAsync(List<ToolCall> toolCalls, CancellationToken cancellationToken = default)
        {
            var results = new List<ToolResult>();

            _logger.LogInformation("Executing {ToolCount} tools in sequence", toolCalls.Count);

            foreach (var toolCall in toolCalls)
            {
                try
                {
                    using var timeoutCts = new CancellationTokenSource(toolCall.TimeoutMs);
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    var result = await ExecuteToolAsync(toolCall.ToolName, toolCall.Parameters, combinedCts.Token);
                    results.Add(result);

                    // If a tool fails, decide whether to continue based on configuration
                    if (!result.Success)
                    {
                        _logger.LogWarning("Tool {ToolName} failed: {Error}", toolCall.ToolName, result.Error?.Message);
                        // For now, continue executing other tools even if one fails
                        // This could be made configurable
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Tool {ToolName} timed out after {TimeoutMs}ms", toolCall.ToolName, toolCall.TimeoutMs);
                    results.Add(ToolResult.CreateError("TIMEOUT", $"Tool '{toolCall.ToolName}' timed out after {toolCall.TimeoutMs}ms"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error executing tool {ToolName}", toolCall.ToolName);
                    results.Add(ToolResult.CreateError("EXECUTION_ERROR", $"Unexpected error executing tool '{toolCall.ToolName}': {ex.Message}"));
                }
            }

            _logger.LogInformation("Completed execution of {ToolCount} tools with {SuccessCount} successes", 
                toolCalls.Count, results.Count(r => r.Success));

            return results;
        }

        public Task<List<ToolSchema>> GetToolSchemasAsync()
        {
            var schemas = new List<ToolSchema>();

            try
            {
                var tools = GetAvailableTools();

                foreach (var tool in tools)
                {
                    try
                    {
                        var schema = new ToolSchema
                        {
                            Name = tool.Name,
                            Description = tool.Description,
                            Parameters = tool.Parameters,
                            Category = GetToolCategory(tool.Name),
                            Examples = GetToolExamples(tool.Name)
                        };

                        schemas.Add(schema);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate schema for tool {ToolName}", tool.Name);
                    }
                }

                _logger.LogInformation("Generated schemas for {SchemaCount} tools", schemas.Count);
                return Task.FromResult(schemas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tool schemas");
                return Task.FromResult(schemas);
            }
        }

        private void RegisterTools()
        {
            // Register all MCP tools here
            // This could be made dynamic by scanning assemblies for IMCPTool implementations

            // Order Management Tools
            RegisterTool<GetUserOrdersTool>("get_user_orders");
            RegisterTool<GetOrderDetailsTool>("get_order_details");
            // Add more tools as they are implemented

            _logger.LogInformation("Registered {ToolCount} MCP tools", _toolTypes.Count);
        }

        private void RegisterTool<T>(string toolName) where T : class, IMCPTool
        {
            _toolTypes[toolName] = typeof(T);
            _logger.LogDebug("Registered tool {ToolName} with type {ToolType}", toolName, typeof(T).Name);
        }

        private string GetToolCategory(string toolName)
        {
            // Categorize tools based on their names or functionality
            return toolName switch
            {
                var name when name.StartsWith("get_user") || name.StartsWith("get_order") => "Order Management",
                var name when name.StartsWith("update_order") || name.StartsWith("create_order") => "Order Operations",
                var name when name.StartsWith("analyze") || name.StartsWith("generate_suggestions") => "AI Analysis",
                var name when name.StartsWith("send_notification") || name.StartsWith("create_alert") => "Communication",
                var name when name.StartsWith("validate") => "Validation",
                _ => "General"
            };
        }

        private List<ToolExample>? GetToolExamples(string toolName)
        {
            // Provide examples for tools
            return toolName switch
            {
                "get_user_orders" => new List<ToolExample>
                {
                    new()
                    {
                        Description = "Get recent orders for a user",
                        Parameters = new { userId = "user123", status = new[] { "pending_l1", "approved" }, limit = 10 },
                        ExpectedResult = new { orders = "Array of order objects", totalCount = "Number of matching orders" }
                    }
                },
                "get_order_details" => new List<ToolExample>
                {
                    new()
                    {
                        Description = "Get complete order details including history",
                        Parameters = new { orderId = "order123", includeHistory = true, includeApprovals = true },
                        ExpectedResult = new { order = "Complete order object", workflowAnalysis = "Workflow status analysis" }
                    }
                },
                _ => null
            };
        }
    }
}
