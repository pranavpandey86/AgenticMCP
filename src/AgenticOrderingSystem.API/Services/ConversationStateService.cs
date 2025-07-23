namespace AgenticOrderingSystem.API.Services;

/// <summary>
/// Service to manage conversation state and context across agent interactions
/// </summary>
public interface IConversationStateService
{
    Task<ConversationState?> GetConversationAsync(string conversationId);
    Task<ConversationState> GetOrCreateConversationAsync(string userId, string? conversationId = null);
    Task SaveConversationAsync(ConversationState conversation);
    Task UpdateConversationAsync(ConversationState conversation);
    Task<string> StartNewConversationAsync(string userId);
    Task ClearConversationAsync(string conversationId);
}

public class ConversationStateService : IConversationStateService
{
    private readonly Dictionary<string, ConversationState> _conversations = new();
    private readonly ILogger<ConversationStateService> _logger;

    public ConversationStateService(ILogger<ConversationStateService> logger)
    {
        _logger = logger;
    }

    public Task<ConversationState?> GetConversationAsync(string conversationId)
    {
        _logger.LogDebug("Attempting to get conversation: {ConversationId}. Total conversations in memory: {Count}", 
            conversationId, _conversations.Count);
        
        _conversations.TryGetValue(conversationId, out var conversation);
        
        if (conversation == null)
        {
            _logger.LogWarning("Conversation not found: {ConversationId}. Available conversations: {ConversationIds}", 
                conversationId, string.Join(", ", _conversations.Keys));
        }
        else
        {
            _logger.LogDebug("Found conversation: {ConversationId}, Last updated: {LastUpdated}", 
                conversationId, conversation.LastUpdated);
        }
        
        return Task.FromResult(conversation);
    }

    public Task SaveConversationAsync(ConversationState conversation)
    {
        _conversations[conversation.Id] = conversation;
        _logger.LogInformation("Saved conversation state: {ConversationId}. Total conversations: {Count}", 
            conversation.Id, _conversations.Count);
        return Task.CompletedTask;
    }

    public async Task<ConversationState> GetOrCreateConversationAsync(string userId, string? conversationId = null)
    {
        if (!string.IsNullOrEmpty(conversationId))
        {
            var existing = await GetConversationAsync(conversationId);
            if (existing != null)
            {
                return existing;
            }
        }

        // Create new conversation
        var newConversationId = await StartNewConversationAsync(userId);
        var newConversation = await GetConversationAsync(newConversationId);
        return newConversation!; // We just created it, so it won't be null
    }

    public Task UpdateConversationAsync(ConversationState conversation)
    {
        conversation.LastUpdated = DateTime.UtcNow;
        return SaveConversationAsync(conversation);
    }

    public Task<string> StartNewConversationAsync(string userId)
    {
        var conversationId = Guid.NewGuid().ToString();
        var conversation = new ConversationState
        {
            Id = conversationId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Messages = new List<ConversationMessage>()
        };
        
        _conversations[conversationId] = conversation;
        _logger.LogInformation("Started new conversation: {ConversationId} for user: {UserId}", conversationId, userId);
        return Task.FromResult(conversationId);
    }

    public Task ClearConversationAsync(string conversationId)
    {
        _conversations.Remove(conversationId);
        _logger.LogDebug("Cleared conversation: {ConversationId}", conversationId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents the state of an ongoing conversation
/// </summary>
public class ConversationState
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<ConversationMessage> Messages { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public string? PendingAction { get; set; }
    public Dictionary<string, object> PendingActionData { get; set; } = new();

    public void AddMessage(string role, string content)
    {
        Messages.Add(new ConversationMessage
        {
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow
        });
        LastUpdated = DateTime.UtcNow;
    }
}

/// <summary>
/// Individual message in a conversation
/// </summary>
public class ConversationMessage
{
    public string Role { get; set; } = string.Empty; // "user", "agent", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
