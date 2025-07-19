using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgenticOrderingSystem.API.Models;

/// <summary>
/// Represents an AI Agent conversation session for managing user interactions and order creation
/// </summary>
public class AIAgentSession
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("sessionId")]
    public string SessionId { get; set; } = string.Empty; // AISESS-2025-001

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty; // Reference to User.Id

    [BsonElement("userInfo")]
    public SessionUserInfo UserInfo { get; set; } = new();

    [BsonElement("status")]
    public string Status { get; set; } = "active"; // active|completed|abandoned|error|escalated

    [BsonElement("intent")]
    public string Intent { get; set; } = "unknown"; // order_request|product_inquiry|support|general

    [BsonElement("currentStep")]
    public string CurrentStep { get; set; } = "greeting"; // greeting|product_selection|information_gathering|review|submission|completion

    [BsonElement("context")]
    public SessionContext Context { get; set; } = new();

    [BsonElement("conversation")]
    public List<ConversationMessage> Conversation { get; set; } = new();

    [BsonElement("extractedEntities")]
    public SessionEntities ExtractedEntities { get; set; } = new();

    [BsonElement("orderDraft")]
    public OrderDraft? OrderDraft { get; set; }

    [BsonElement("relatedOrderIds")]
    public List<string> RelatedOrderIds { get; set; } = new();

    [BsonElement("aiConfiguration")]
    public AIConfiguration AiConfiguration { get; set; } = new();

    [BsonElement("metadata")]
    public SessionMetadata Metadata { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastInteractionAt")]
    public DateTime LastInteractionAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("timeoutAt")]
    public DateTime TimeoutAt { get; set; } = DateTime.UtcNow.AddHours(4); // Default 4-hour timeout

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// User information for the session
/// </summary>
public class SessionUserInfo
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("department")]
    public string Department { get; set; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } = string.Empty;

    [BsonElement("preferences")]
    public Dictionary<string, string> Preferences { get; set; } = new();

    [BsonElement("approvalAuthority")]
    public decimal ApprovalAuthority { get; set; } = 0;

    [BsonElement("timeZone")]
    public string TimeZone { get; set; } = "UTC";

    [BsonElement("language")]
    public string Language { get; set; } = "en";
}

/// <summary>
/// Session context and state information
/// </summary>
public class SessionContext
{
    [BsonElement("channel")]
    public string Channel { get; set; } = "web"; // web|mobile|api|slack|teams

    [BsonElement("deviceInfo")]
    public string DeviceInfo { get; set; } = string.Empty;

    [BsonElement("browserInfo")]
    public string BrowserInfo { get; set; } = string.Empty;

    [BsonElement("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    [BsonElement("location")]
    public SessionLocation? Location { get; set; }

    [BsonElement("sessionVariables")]
    public Dictionary<string, string> SessionVariables { get; set; } = new();

    [BsonElement("conversationHistory")]
    public List<string> ConversationHistory { get; set; } = new(); // Recent topic history

    [BsonElement("userIntent")]
    public string UserIntent { get; set; } = string.Empty;

    [BsonElement("confidence")]
    public decimal Confidence { get; set; } = 0;

    [BsonElement("escalationTriggers")]
    public List<string> EscalationTriggers { get; set; } = new();
}

/// <summary>
/// User location information
/// </summary>
public class SessionLocation
{
    [BsonElement("country")]
    public string Country { get; set; } = string.Empty;

    [BsonElement("region")]
    public string Region { get; set; } = string.Empty;

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("timeZone")]
    public string TimeZone { get; set; } = string.Empty;
}

/// <summary>
/// Individual conversation message
/// </summary>
public class ConversationMessage
{
    [BsonElement("messageId")]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("sender")]
    public string Sender { get; set; } = string.Empty; // user|agent|system

    [BsonElement("senderName")]
    public string SenderName { get; set; } = string.Empty;

    [BsonElement("messageType")]
    public string MessageType { get; set; } = "text"; // text|action|system|error|escalation

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("intent")]
    public string Intent { get; set; } = string.Empty;

    [BsonElement("entities")]
    public List<MessageEntity> Entities { get; set; } = new();

    [BsonElement("attachments")]
    public List<MessageAttachment> Attachments { get; set; } = new();

    [BsonElement("aiResponse")]
    public AIResponseMetadata? AiResponse { get; set; }

    [BsonElement("isUserVisible")]
    public bool IsUserVisible { get; set; } = true;

    [BsonElement("isSystemMessage")]
    public bool IsSystemMessage { get; set; } = false;
}

/// <summary>
/// Extracted entities from messages
/// </summary>
public class MessageEntity
{
    [BsonElement("entityType")]
    public string EntityType { get; set; } = string.Empty; // product|category|amount|date|person|location

    [BsonElement("entityValue")]
    public string EntityValue { get; set; } = string.Empty;

    [BsonElement("confidence")]
    public decimal Confidence { get; set; } = 0;

    [BsonElement("startPosition")]
    public int StartPosition { get; set; } = 0;

    [BsonElement("endPosition")]
    public int EndPosition { get; set; } = 0;

    [BsonElement("resolvedValue")]
    public string? ResolvedValue { get; set; }
}

/// <summary>
/// Message attachments
/// </summary>
public class MessageAttachment
{
    [BsonElement("attachmentId")]
    public string AttachmentId { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    [BsonElement("fileType")]
    public string FileType { get; set; } = string.Empty;

    [BsonElement("fileSize")]
    public long FileSize { get; set; }

    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// AI response metadata
/// </summary>
public class AIResponseMetadata
{
    [BsonElement("model")]
    public string Model { get; set; } = string.Empty; // perplexity-70b|gpt-4|claude-3

    [BsonElement("tokensUsed")]
    public int TokensUsed { get; set; } = 0;

    [BsonElement("responseTime")]
    public double ResponseTime { get; set; } = 0; // milliseconds

    [BsonElement("confidence")]
    public decimal Confidence { get; set; } = 0;

    [BsonElement("cost")]
    public decimal Cost { get; set; } = 0;

    [BsonElement("apiVersion")]
    public string ApiVersion { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Extracted entities throughout the session
/// </summary>
public class SessionEntities
{
    [BsonElement("products")]
    public List<ExtractedProduct> Products { get; set; } = new();

    [BsonElement("categories")]
    public List<ExtractedCategory> Categories { get; set; } = new();

    [BsonElement("amounts")]
    public List<ExtractedAmount> Amounts { get; set; } = new();

    [BsonElement("dates")]
    public List<ExtractedDate> Dates { get; set; } = new();

    [BsonElement("departments")]
    public List<string> Departments { get; set; } = new();

    [BsonElement("people")]
    public List<ExtractedPerson> People { get; set; } = new();

    [BsonElement("businessJustifications")]
    public List<string> BusinessJustifications { get; set; } = new();

    [BsonElement("requirements")]
    public List<string> Requirements { get; set; } = new();
}

/// <summary>
/// Extracted product information
/// </summary>
public class ExtractedProduct
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("productId")]
    public string? ProductId { get; set; }

    [BsonElement("confidence")]
    public decimal Confidence { get; set; } = 0;

    [BsonElement("mentionedAt")]
    public DateTime MentionedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("context")]
    public string Context { get; set; } = string.Empty;
}

/// <summary>
/// Extracted category information
/// </summary>
public class ExtractedCategory
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("categoryId")]
    public string? CategoryId { get; set; }

    [BsonElement("confidence")]
    public decimal Confidence { get; set; } = 0;

    [BsonElement("mentionedAt")]
    public DateTime MentionedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Extracted amount information
/// </summary>
public class ExtractedAmount
{
    [BsonElement("amount")]
    public decimal Amount { get; set; } = 0;

    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";

    [BsonElement("context")]
    public string Context { get; set; } = string.Empty; // budget|cost|limit

    [BsonElement("confidence")]
    public decimal Confidence { get; set; } = 0;

    [BsonElement("mentionedAt")]
    public DateTime MentionedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Extracted date information
/// </summary>
public class ExtractedDate
{
    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("dateType")]
    public string DateType { get; set; } = string.Empty; // deadline|required_by|start_date

    [BsonElement("confidence")]
    public decimal Confidence { get; set; } = 0;

    [BsonElement("mentionedAt")]
    public DateTime MentionedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("context")]
    public string Context { get; set; } = string.Empty;
}

/// <summary>
/// Extracted person information
/// </summary>
public class ExtractedPerson
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string? UserId { get; set; }

    [BsonElement("role")]
    public string Role { get; set; } = string.Empty; // manager|approver|stakeholder

    [BsonElement("confidence")]
    public decimal Confidence { get; set; } = 0;

    [BsonElement("mentionedAt")]
    public DateTime MentionedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Draft order being built during the session
/// </summary>
public class OrderDraft
{
    [BsonElement("productId")]
    public string? ProductId { get; set; }

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("estimatedAmount")]
    public decimal EstimatedAmount { get; set; } = 0;

    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";

    [BsonElement("businessJustification")]
    public string BusinessJustification { get; set; } = string.Empty;

    [BsonElement("requiredByDate")]
    public DateTime? RequiredByDate { get; set; }

    [BsonElement("priority")]
    public string Priority { get; set; } = "medium";

    [BsonElement("customResponses")]
    public Dictionary<string, string> CustomResponses { get; set; } = new();

    [BsonElement("deliveryInfo")]
    public Dictionary<string, string> DeliveryInfo { get; set; } = new();

    [BsonElement("isComplete")]
    public bool IsComplete { get; set; } = false;

    [BsonElement("validationErrors")]
    public List<string> ValidationErrors { get; set; } = new();

    [BsonElement("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// AI configuration for the session
/// </summary>
public class AIConfiguration
{
    [BsonElement("model")]
    public string Model { get; set; } = "perplexity-70b";

    [BsonElement("temperature")]
    public decimal Temperature { get; set; } = 0.7m;

    [BsonElement("maxTokens")]
    public int MaxTokens { get; set; } = 4000;

    [BsonElement("enableEntityExtraction")]
    public bool EnableEntityExtraction { get; set; } = true;

    [BsonElement("enableIntentRecognition")]
    public bool EnableIntentRecognition { get; set; } = true;

    [BsonElement("enableAutoCompletion")]
    public bool EnableAutoCompletion { get; set; } = true;

    [BsonElement("systemPrompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [BsonElement("conversationStyle")]
    public string ConversationStyle { get; set; } = "professional"; // casual|professional|technical

    [BsonElement("escalationThreshold")]
    public decimal EscalationThreshold { get; set; } = 0.3m; // Confidence threshold for human escalation
}

/// <summary>
/// Session metadata and analytics
/// </summary>
public class SessionMetadata
{
    [BsonElement("source")]
    public string Source { get; set; } = "web_portal";

    [BsonElement("referrer")]
    public string? Referrer { get; set; }

    [BsonElement("campaign")]
    public string? Campaign { get; set; }

    [BsonElement("messageCount")]
    public int MessageCount { get; set; } = 0;

    [BsonElement("aiResponseCount")]
    public int AiResponseCount { get; set; } = 0;

    [BsonElement("userSatisfactionScore")]
    public decimal? UserSatisfactionScore { get; set; } // 1-5 rating

    [BsonElement("sessionDuration")]
    public double SessionDuration { get; set; } = 0; // minutes

    [BsonElement("totalCost")]
    public decimal TotalCost { get; set; } = 0; // API costs

    [BsonElement("escalatedToHuman")]
    public bool EscalatedToHuman { get; set; } = false;

    [BsonElement("humanHandoffAt")]
    public DateTime? HumanHandoffAt { get; set; }

    [BsonElement("humanAgentId")]
    public string? HumanAgentId { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("analytics")]
    public Dictionary<string, object> Analytics { get; set; } = new();
}
