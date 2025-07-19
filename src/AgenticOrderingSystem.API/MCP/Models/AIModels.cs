using System.Text.Json.Serialization;

namespace AgenticOrderingSystem.API.MCP.Models
{
    /// <summary>
    /// Request to Perplexity AI
    /// </summary>
    public class AIRequest
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("context")]
        public AIContext? Context { get; set; }

        [JsonPropertyName("intent")]
        public string? Intent { get; set; }

        [JsonPropertyName("tools")]
        public List<ToolSchema>? AvailableTools { get; set; }

        [JsonPropertyName("maxTokens")]
        public int? MaxTokens { get; set; } = 1000;

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; } = 0.7;
    }

    /// <summary>
    /// Response from Perplexity AI
    /// </summary>
    public class AIResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("toolCalls")]
        public List<ToolCall>? ToolCalls { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("intent")]
        public string? DetectedIntent { get; set; }

        [JsonPropertyName("metadata")]
        public AIResponseMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Context for AI requests
    /// </summary>
    public class AIContext
    {
        [JsonPropertyName("userId")]
        public string? UserId { get; set; }

        [JsonPropertyName("orderId")]
        public string? OrderId { get; set; }

        [JsonPropertyName("conversationHistory")]
        public List<ConversationMessage>? ConversationHistory { get; set; }

        [JsonPropertyName("userProfile")]
        public object? UserProfile { get; set; }

        [JsonPropertyName("sessionData")]
        public Dictionary<string, object>? SessionData { get; set; }
    }

    /// <summary>
    /// Conversation message
    /// </summary>
    public class ConversationMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("sender")]
        public string Sender { get; set; } = string.Empty; // "user" or "agent"

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "text"; // text, action, system, error, suggestion

        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Metadata for AI responses
    /// </summary>
    public class AIResponseMetadata
    {
        [JsonPropertyName("processingTimeMs")]
        public long ProcessingTimeMs { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("tokensUsed")]
        public TokenUsage? TokensUsed { get; set; }

        [JsonPropertyName("cost")]
        public double? EstimatedCost { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Token usage information
    /// </summary>
    public class TokenUsage
    {
        [JsonPropertyName("inputTokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("outputTokens")]
        public int OutputTokens { get; set; }

        [JsonPropertyName("totalTokens")]
        public int TotalTokens { get; set; }
    }

    /// <summary>
    /// Analysis of order rejection
    /// </summary>
    public class RejectionAnalysis
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("overallReason")]
        public string OverallReason { get; set; } = string.Empty;

        [JsonPropertyName("specificIssues")]
        public List<SpecificIssue> SpecificIssues { get; set; } = new();

        [JsonPropertyName("suggestions")]
        public List<AIsuggestion> Suggestions { get; set; } = new();

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("similarCases")]
        public List<SimilarCase>? SimilarCases { get; set; }

        [JsonPropertyName("estimatedFixTimeMinutes")]
        public int EstimatedFixTimeMinutes { get; set; }
    }

    /// <summary>
    /// Specific issue in an order
    /// </summary>
    public class SpecificIssue
    {
        [JsonPropertyName("questionKey")]
        public string QuestionKey { get; set; } = string.Empty;

        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("currentAnswer")]
        public object? CurrentAnswer { get; set; }

        [JsonPropertyName("issueDescription")]
        public string IssueDescription { get; set; } = string.Empty;

        [JsonPropertyName("suggestedAction")]
        public string SuggestedAction { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = string.Empty; // high, medium, low

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty; // validation, policy, business
    }

    /// <summary>
    /// AI-generated suggestion
    /// </summary>
    public class AIsuggestion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty; // high, medium, low

        [JsonPropertyName("estimatedImpact")]
        public string EstimatedImpact { get; set; } = string.Empty;

        [JsonPropertyName("requiredFields")]
        public List<string>? RequiredFields { get; set; }

        [JsonPropertyName("proposedValues")]
        public Dictionary<string, object>? ProposedValues { get; set; }
    }

    /// <summary>
    /// Similar case for reference
    /// </summary>
    public class SimilarCase
    {
        [JsonPropertyName("caseId")]
        public string CaseId { get; set; } = string.Empty;

        [JsonPropertyName("similarity")]
        public double Similarity { get; set; }

        [JsonPropertyName("outcome")]
        public string Outcome { get; set; } = string.Empty;

        [JsonPropertyName("resolution")]
        public string Resolution { get; set; } = string.Empty;

        [JsonPropertyName("timeToResolve")]
        public int TimeToResolveMinutes { get; set; }
    }

    /// <summary>
    /// Response for conversation with tool usage
    /// </summary>
    public class ConversationResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("toolCalls")]
        public List<ToolCall>? ToolCalls { get; set; }

        [JsonPropertyName("nextActions")]
        public List<string>? NextActions { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("intent")]
        public string? DetectedIntent { get; set; }

        [JsonPropertyName("conversationComplete")]
        public bool ConversationComplete { get; set; }

        [JsonPropertyName("metadata")]
        public AIResponseMetadata Metadata { get; set; } = new();
    }
}
