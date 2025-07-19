using Microsoft.AspNetCore.Mvc;
using AgenticOrderingSystem.API.MCP.Interfaces;
using AgenticOrderingSystem.API.MCP.Models;

namespace AgenticOrderingSystem.API.Controllers
{
    /// <summary>
    /// Controller for MCP (Model Context Protocol) operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MCPController : ControllerBase
    {
        private readonly IMCPOrchestrator _mcpOrchestrator;
        private readonly IPerplexityAIService _aiService;
        private readonly ILogger<MCPController> _logger;

        public MCPController(
            IMCPOrchestrator mcpOrchestrator,
            IPerplexityAIService aiService,
            ILogger<MCPController> logger)
        {
            _mcpOrchestrator = mcpOrchestrator;
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Get all available MCP tools and their schemas
        /// </summary>
        /// <returns>List of available tools with their schemas</returns>
        [HttpGet("tools")]
        public async Task<IActionResult> GetToolSchemas()
        {
            try
            {
                _logger.LogInformation("Retrieving MCP tool schemas");

                var schemas = await _mcpOrchestrator.GetToolSchemasAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        tools = schemas,
                        totalCount = schemas.Count,
                        categories = schemas.GroupBy(s => s.Category)
                            .ToDictionary(g => g.Key, g => g.Count())
                    },
                    message = "Tool schemas retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tool schemas");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to retrieve tool schemas",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Execute a specific MCP tool with parameters
        /// </summary>
        /// <param name="toolName">Name of the tool to execute</param>
        /// <param name="parameters">Tool parameters</param>
        /// <returns>Tool execution result</returns>
        [HttpPost("tools/{toolName}/execute")]
        public async Task<IActionResult> ExecuteTool(string toolName, [FromBody] object parameters)
        {
            try
            {
                _logger.LogInformation("Executing MCP tool {ToolName}", toolName);

                var result = await _mcpOrchestrator.ExecuteToolAsync(toolName, parameters);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        data = result.Data,
                        metadata = result.Metadata,
                        message = "Tool executed successfully"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = result.Error,
                        metadata = result.Metadata,
                        message = "Tool execution failed"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to execute tool",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Execute multiple MCP tools in sequence
        /// </summary>
        /// <param name="toolCalls">List of tool calls to execute</param>
        /// <returns>List of tool execution results</returns>
        [HttpPost("tools/execute-batch")]
        public async Task<IActionResult> ExecuteTools([FromBody] List<ToolCall> toolCalls)
        {
            try
            {
                _logger.LogInformation("Executing {ToolCount} MCP tools in batch", toolCalls.Count);

                var results = await _mcpOrchestrator.ExecuteToolsAsync(toolCalls);

                var successCount = results.Count(r => r.Success);
                var failureCount = results.Count - successCount;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        results,
                        summary = new
                        {
                            totalTools = toolCalls.Count,
                            successCount,
                            failureCount,
                            successRate = successCount / (double)toolCalls.Count
                        }
                    },
                    message = $"Executed {toolCalls.Count} tools with {successCount} successes"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing batch tools");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to execute batch tools",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Send a message to the AI agent and get a response
        /// </summary>
        /// <param name="request">AI request with message and context</param>
        /// <returns>AI response</returns>
        [HttpPost("ai/chat")]
        public async Task<IActionResult> SendAIMessage([FromBody] AIRequest request)
        {
            try
            {
                _logger.LogInformation("Sending message to AI for session {SessionId}", request.SessionId);

                var response = await _aiService.SendMessageAsync(request);

                return Ok(new
                {
                    success = true,
                    data = response,
                    message = "AI response generated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI response");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to get AI response",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get a conversational response from AI with tool usage capability
        /// </summary>
        /// <param name="request">Conversation request</param>
        /// <returns>Conversation response with potential tool calls</returns>
        [HttpPost("ai/conversation")]
        public async Task<IActionResult> GetConversationResponse([FromBody] ConversationRequest request)
        {
            try
            {
                _logger.LogInformation("Getting conversation response for session {SessionId}", request.SessionId);

                // Get available tools for the AI
                var availableTools = await _mcpOrchestrator.GetToolSchemasAsync();

                var response = await _aiService.GetConversationResponseAsync(
                    request.SessionId, 
                    request.Message, 
                    availableTools);

                // If the AI wants to call tools, execute them
                if (response.ToolCalls?.Any() == true)
                {
                    _logger.LogInformation("AI requested {ToolCount} tool calls", response.ToolCalls.Count);
                    
                    var toolResults = await _mcpOrchestrator.ExecuteToolsAsync(response.ToolCalls);
                    
                    // You could send the tool results back to AI for a follow-up response
                    // For now, we'll include them in the response
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            conversationResponse = response,
                            toolResults,
                            toolExecutionSummary = new
                            {
                                toolsExecuted = toolResults.Count,
                                successfulTools = toolResults.Count(r => r.Success),
                                failedTools = toolResults.Count(r => !r.Success)
                            }
                        },
                        message = "Conversation response with tool execution completed"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = response,
                    message = "Conversation response generated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation response");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to get conversation response",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Analyze an order rejection using AI
        /// </summary>
        /// <param name="orderId">Order ID to analyze</param>
        /// <param name="rejectionDetails">Rejection details</param>
        /// <returns>AI analysis of the rejection</returns>
        [HttpPost("ai/analyze-rejection/{orderId}")]
        public async Task<IActionResult> AnalyzeRejection(string orderId, [FromBody] object rejectionDetails)
        {
            try
            {
                _logger.LogInformation("Analyzing rejection for order {OrderId}", orderId);

                var analysis = await _aiService.AnalyzeRejectionAsync(orderId, rejectionDetails);

                return Ok(new
                {
                    success = true,
                    data = analysis,
                    message = "Rejection analysis completed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing rejection for order {OrderId}", orderId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to analyze rejection",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Generate AI suggestions for fixing order issues
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <param name="request">Problem context</param>
        /// <returns>AI-generated suggestions</returns>
        [HttpPost("ai/generate-suggestions/{orderId}")]
        public async Task<IActionResult> GenerateSuggestions(string orderId, [FromBody] GenerateSuggestionsRequest request)
        {
            try
            {
                _logger.LogInformation("Generating suggestions for order {OrderId}", orderId);

                var suggestions = await _aiService.GenerateSuggestionsAsync(orderId, request.ProblemContext);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        orderId,
                        suggestions,
                        totalSuggestions = suggestions.Count,
                        prioritySuggestions = suggestions.Where(s => s.Priority == "high").ToList()
                    },
                    message = "Suggestions generated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating suggestions for order {OrderId}", orderId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to generate suggestions",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get MCP server health and status
        /// </summary>
        /// <returns>MCP server status information</returns>
        [HttpGet("health")]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var availableTools = _mcpOrchestrator.GetAvailableTools().ToList();
                var toolSchemas = await _mcpOrchestrator.GetToolSchemasAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        status = "healthy",
                        timestamp = DateTime.UtcNow,
                        mcpServer = new
                        {
                            availableTools = availableTools.Count,
                            toolCategories = toolSchemas.GroupBy(s => s.Category).Count(),
                            version = "1.0.0"
                        },
                        aiService = new
                        {
                            status = "connected",
                            provider = "Perplexity AI"
                        }
                    },
                    message = "MCP server is healthy and operational"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking MCP health");
                return StatusCode(500, new
                {
                    success = false,
                    message = "MCP server health check failed",
                    error = ex.Message
                });
            }
        }
    }

    /// <summary>
    /// Request model for conversation endpoint
    /// </summary>
    public class ConversationRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AIContext? Context { get; set; }
    }

    /// <summary>
    /// Request model for generating suggestions
    /// </summary>
    public class GenerateSuggestionsRequest
    {
        public string ProblemContext { get; set; } = string.Empty;
    }
}
