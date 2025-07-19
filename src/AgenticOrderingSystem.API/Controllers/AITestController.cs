using Microsoft.AspNetCore.Mvc;
using AgenticOrderingSystem.API.MCP.Interfaces;
using AgenticOrderingSystem.API.MCP.Models;

namespace AgenticOrderingSystem.API.Controllers
{
    /// <summary>
    /// Controller for testing AI integrations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AITestController : ControllerBase
    {
        private readonly IPerplexityAIService _perplexityService;
        private readonly ILogger<AITestController> _logger;

        public AITestController(IPerplexityAIService perplexityService, ILogger<AITestController> logger)
        {
            _perplexityService = perplexityService;
            _logger = logger;
        }

        /// <summary>
        /// Test basic Perplexity AI connection
        /// </summary>
        [HttpPost("perplexity/simple")]
        public async Task<IActionResult> TestSimplePerplexityConnection([FromBody] SimpleTestRequest request)
        {
            try
            {
                _logger.LogInformation("Testing Perplexity AI with message: {Message}", request.Message);

                var aiRequest = new AIRequest
                {
                    SessionId = $"test-{Guid.NewGuid()}",
                    Message = request.Message ?? "Hello, can you respond with a simple greeting?",
                    MaxTokens = 100,
                    Temperature = 0.7
                };

                var response = await _perplexityService.SendMessageAsync(aiRequest);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        message = response.Message,
                        confidence = response.Confidence,
                        model = response.Metadata.Model,
                        tokensUsed = response.Metadata.TokensUsed,
                        requestId = response.Metadata.RequestId
                    },
                    message = "Perplexity AI test completed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Perplexity AI connection");
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    message = "Failed to connect to Perplexity AI"
                });
            }
        }

        /// <summary>
        /// Test Perplexity AI with conversation context
        /// </summary>
        [HttpPost("perplexity/conversation")]
        public async Task<IActionResult> TestConversationWithContext([FromBody] ConversationTestRequest request)
        {
            try
            {
                _logger.LogInformation("Testing Perplexity AI conversation for session {SessionId}", request.SessionId);

                var aiRequest = new AIRequest
                {
                    SessionId = request.SessionId ?? $"conv-test-{Guid.NewGuid()}",
                    Message = request.Message ?? "What can you help me with?",
                    Context = new AIContext
                    {
                        ConversationHistory = request.History,
                        SessionData = new Dictionary<string, object>
                        {
                            ["testMode"] = true,
                            ["timestamp"] = DateTime.UtcNow
                        }
                    },
                    MaxTokens = request.MaxTokens ?? 200,
                    Temperature = request.Temperature ?? 0.7
                };

                var response = await _perplexityService.SendMessageAsync(aiRequest);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        sessionId = aiRequest.SessionId,
                        message = response.Message,
                        confidence = response.Confidence,
                        detectedIntent = response.DetectedIntent,
                        metadata = response.Metadata
                    },
                    message = "Conversation test completed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Perplexity AI conversation");
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    message = "Failed to test conversation with Perplexity AI"
                });
            }
        }

        /// <summary>
        /// Test AI connection status without making API calls
        /// </summary>
        [HttpGet("perplexity/status")]
        public IActionResult GetPerplexityStatus()
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("PERPLEXITY_API_KEY");
                var baseUrl = Environment.GetEnvironmentVariable("PERPLEXITY_BASE_URL") ?? "https://api.perplexity.ai";

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        apiKeyConfigured = !string.IsNullOrEmpty(apiKey),
                        apiKeyPreview = string.IsNullOrEmpty(apiKey) ? null : $"{apiKey[..4]}...{apiKey[^4..]}",
                        baseUrl = baseUrl,
                        serviceRegistered = _perplexityService != null,
                        timestamp = DateTime.UtcNow
                    },
                    message = "Perplexity AI service status retrieved"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Perplexity AI status");
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    message = "Failed to get Perplexity AI status"
                });
            }
        }
    }

    /// <summary>
    /// Simple test request model
    /// </summary>
    public class SimpleTestRequest
    {
        public string? Message { get; set; }
    }

    /// <summary>
    /// Conversation test request model
    /// </summary>
    public class ConversationTestRequest
    {
        public string? SessionId { get; set; }
        public string? Message { get; set; }
        public List<ConversationMessage>? History { get; set; }
        public int? MaxTokens { get; set; }
        public double? Temperature { get; set; }
    }
}
