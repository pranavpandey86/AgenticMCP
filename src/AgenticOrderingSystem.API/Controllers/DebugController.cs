using Microsoft.AspNetCore.Mvc;
using AgenticOrderingSystem.API.MCP.Interfaces;
using AgenticOrderingSystem.API.MCP.Models;
using AgenticOrderingSystem.API.MCP.Tools.OrderManagement;

namespace AgenticOrderingSystem.API.Controllers
{
    /// <summary>
    /// Debug controller for testing MCP components
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMCPOrchestrator _mcpOrchestrator;
        private readonly ILogger<DebugController> _logger;

        public DebugController(
            IServiceProvider serviceProvider,
            IMCPOrchestrator mcpOrchestrator,
            ILogger<DebugController> logger)
        {
            _serviceProvider = serviceProvider;
            _mcpOrchestrator = mcpOrchestrator;
            _logger = logger;
        }

        /// <summary>
        /// Test manual tool execution
        /// </summary>
        [HttpPost("test-manual-tool")]
        public async Task<IActionResult> TestManualToolExecution([FromBody] object parameters)
        {
            try
            {
                // Manually get and execute a tool
                var userOrdersTool = _serviceProvider.GetRequiredService<GetUserOrdersTool>();
                
                var result = await userOrdersTool.ExecuteAsync(parameters);
                
                return Ok(new
                {
                    toolName = userOrdersTool.Name,
                    toolDescription = userOrdersTool.Description,
                    executionResult = new
                    {
                        success = result.Success,
                        data = result.Data,
                        error = result.Error,
                        metadata = result.Metadata
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
        [HttpGet("test-tools")]
        public IActionResult TestToolInstantiation()
        {
            try
            {
                var results = new List<object>();

                // Test GetUserOrdersTool
                try
                {
                    var userOrdersTool = _serviceProvider.GetRequiredService<GetUserOrdersTool>();
                    results.Add(new { tool = "GetUserOrdersTool", status = "success", name = userOrdersTool.Name });
                }
                catch (Exception ex)
                {
                    results.Add(new { tool = "GetUserOrdersTool", status = "error", error = ex.Message });
                }

                // Test GetOrderDetailsTool
                try
                {
                    var orderDetailsTool = _serviceProvider.GetRequiredService<GetOrderDetailsTool>();
                    results.Add(new { tool = "GetOrderDetailsTool", status = "success", name = orderDetailsTool.Name });
                }
                catch (Exception ex)
                {
                    results.Add(new { tool = "GetOrderDetailsTool", status = "error", error = ex.Message });
                }

                // Test orchestrator tool registration
                var availableTools = _mcpOrchestrator.GetAvailableTools().ToList();
                
                // Test getting a specific tool
                var getUserOrdersTool = _mcpOrchestrator.GetTool("get_user_orders");
                var getOrderDetailsTool = _mcpOrchestrator.GetTool("get_order_details");
                
                return Ok(new
                {
                    directInstantiation = results,
                    orchestratorTools = availableTools.Count,
                    toolNames = availableTools.Select(t => t.Name).ToList(),
                    specificToolTests = new
                    {
                        getUserOrdersTool = getUserOrdersTool?.Name ?? "null",
                        getOrderDetailsTool = getOrderDetailsTool?.Name ?? "null"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in debug test");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Test Perplexity AI service directly
        /// </summary>
        [HttpPost("test-ai")]
        public async Task<IActionResult> TestAI([FromBody] string message)
        {
            try
            {
                var aiService = _serviceProvider.GetRequiredService<IPerplexityAIService>();
                
                var request = new AIRequest
                {
                    SessionId = "debug-test",
                    Message = message ?? "Hello! Can you help me test the AI service?",
                    Context = new AIContext
                    {
                        UserId = "debug-user"
                    }
                };

                var response = await aiService.SendMessageAsync(request);
                
                return Ok(new
                {
                    request = new
                    {
                        message = request.Message,
                        sessionId = request.SessionId
                    },
                    response = new
                    {
                        message = response.Message,
                        confidence = response.Confidence,
                        intent = response.DetectedIntent,
                        toolCalls = response.ToolCalls?.Count ?? 0,
                        metadata = response.Metadata
                    },
                    debugging = new
                    {
                        apiKeyConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PERPLEXITY_API_KEY")),
                        baseUrl = Environment.GetEnvironmentVariable("PERPLEXITY_BASE_URL")
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = ex.Message, 
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                });
            }
        }
        [HttpGet("test-orchestrator")]
        public IActionResult TestOrchestrator()
        {
            try
            {
                // Test if we can get the orchestrator instance and check its state
                var orchestrator = _serviceProvider.GetRequiredService<IMCPOrchestrator>();
                
                // Cast to concrete type to access internal state (for debugging)
                if (orchestrator is AgenticOrderingSystem.API.MCP.Services.MCPOrchestrator mcpOrchestrator)
                {
                    // Use reflection to get the _toolTypes field
                    var toolTypesField = mcpOrchestrator.GetType().GetField("_toolTypes", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    var toolTypes = toolTypesField?.GetValue(mcpOrchestrator) as Dictionary<string, Type>;
                    
                    return Ok(new
                    {
                        orchestratorType = orchestrator.GetType().Name,
                        isSameInstance = ReferenceEquals(orchestrator, _mcpOrchestrator),
                        toolTypesCount = toolTypes?.Count ?? -1,
                        toolTypeNames = toolTypes?.Keys.ToList() ?? new List<string>(),
                        directToolTest = new
                        {
                            getUserOrders = orchestrator.GetTool("get_user_orders")?.Name ?? "null",
                            getOrderDetails = orchestrator.GetTool("get_order_details")?.Name ?? "null"
                        },
                        // Test direct type resolution
                        directTypeResolution = new
                        {
                            getUserOrdersType = TestDirectTypeResolution(toolTypes, "get_user_orders"),
                            getOrderDetailsType = TestDirectTypeResolution(toolTypes, "get_order_details")
                        }
                    });
                }
                
                return Ok(new
                {
                    orchestratorType = orchestrator.GetType().Name,
                    isSameInstance = ReferenceEquals(orchestrator, _mcpOrchestrator),
                    note = "Could not cast to concrete type for internal inspection"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
        [HttpGet("test-services")]
        public IActionResult TestServices()
        {
            try
            {
                var services = new List<object>();

                // Test all relevant services
                var serviceTypes = new[]
                {
                    typeof(IMCPOrchestrator),
                    typeof(IPerplexityAIService),
                    typeof(GetUserOrdersTool),
                    typeof(GetOrderDetailsTool),
                    typeof(Services.IOrderService),
                    typeof(Services.IUserService)
                };

                foreach (var serviceType in serviceTypes)
                {
                    try
                    {
                        var service = _serviceProvider.GetRequiredService(serviceType);
                        services.Add(new { 
                            serviceType = serviceType.Name, 
                            status = "registered", 
                            implementation = service.GetType().Name 
                        });
                    }
                    catch (Exception ex)
                    {
                        services.Add(new { 
                            serviceType = serviceType.Name, 
                            status = "not_registered", 
                            error = ex.Message 
                        });
                    }
                }

                return Ok(new { services });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private object TestDirectTypeResolution(Dictionary<string, Type>? toolTypes, string toolName)
        {
            try
            {
                if (toolTypes == null || !toolTypes.TryGetValue(toolName, out var toolType))
                {
                    return new { status = "not_found", toolName };
                }

                // Try to resolve the type directly
                var instance = _serviceProvider.GetRequiredService(toolType);
                return new { 
                    status = "success", 
                    toolName, 
                    typeName = toolType.Name,
                    instanceType = instance.GetType().Name
                };
            }
            catch (Exception ex)
            {
                return new { 
                    status = "error", 
                    toolName, 
                    error = ex.Message,
                    exceptionType = ex.GetType().Name
                };
            }
        }
    }
}
