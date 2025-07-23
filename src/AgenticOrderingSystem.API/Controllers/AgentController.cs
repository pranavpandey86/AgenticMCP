using Microsoft.AspNetCore.Mvc;
using AgenticOrderingSystem.API.Services;
using System.ComponentModel.DataAnnotations;

namespace AgenticOrderingSystem.API.Controllers;

/// <summary>
/// Controller for agent-orchestrated conversations and actions
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentOrchestratorService _agentService;
    private readonly ILogger<AgentController> _logger;

    public AgentController(IAgentOrchestratorService agentService, ILogger<AgentController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>
    /// Handle a chat message and return agent response
    /// </summary>
    [HttpPost("chat")]
    public async Task<ActionResult<AgentChatResponse>> HandleChatMessage([FromBody] AgentChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty");
            }

            // For demo purposes, using a hardcoded user ID
            // In a real app, extract from authentication context
            var userId = request.UserId ?? "demo-user-001";

            var response = await _agentService.HandleChatMessageAsync(userId, request.Message, request.ConversationId);

            return Ok(new AgentChatResponse
            {
                Message = response.Message,
                ConversationId = response.ConversationId,
                RequiresConfirmation = response.RequiresConfirmation,
                Data = response.Data,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling chat message");
            return StatusCode(500, new { error = "Failed to process chat message", message = ex.Message });
        }
    }

    /// <summary>
    /// Handle user confirmation for pending actions
    /// </summary>
    [HttpPost("confirm")]
    public async Task<ActionResult<AgentChatResponse>> HandleConfirmation([FromBody] AgentConfirmationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ConversationId))
            {
                return BadRequest("ConversationId is required");
            }

            // For demo purposes, using a hardcoded user ID
            var userId = request.UserId ?? "demo-user-001";

            var response = await _agentService.HandleConfirmationAsync(userId, request.ConversationId, request.Confirmed);

            return Ok(new AgentChatResponse
            {
                Message = response.Message,
                ConversationId = response.ConversationId,
                RequiresConfirmation = response.RequiresConfirmation,
                Data = response.Data,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling confirmation");
            return StatusCode(500, new { error = "Failed to process confirmation", message = ex.Message });
        }
    }

    /// <summary>
    /// Get conversation history
    /// </summary>
    [HttpGet("conversations/{conversationId}")]
    public async Task<ActionResult<ConversationHistoryResponse>> GetConversation(string conversationId)
    {
        try
        {
            // This would need to be implemented in the conversation service
            return Ok(new ConversationHistoryResponse
            {
                ConversationId = conversationId,
                Messages = new List<ConversationMessageDto>(),
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Failed to retrieve conversation", message = ex.Message });
        }
    }
}

#region Request/Response Models

public class AgentChatRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public string? ConversationId { get; set; }
    
    public string? UserId { get; set; }
}

public class AgentConfirmationRequest
{
    [Required]
    public string ConversationId { get; set; } = string.Empty;
    
    [Required]
    public bool Confirmed { get; set; }
    
    public string? UserId { get; set; }
}

public class AgentChatResponse
{
    public string Message { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public bool RequiresConfirmation { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class ConversationHistoryResponse
{
    public string ConversationId { get; set; } = string.Empty;
    public List<ConversationMessageDto> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ConversationMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

#endregion
