using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgenticOrderingSystem.API.Models;

/// <summary>
/// Represents an order in the agentic ordering system with full approval workflow tracking
/// </summary>
public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty; // AUTO-ORD-2025-001

    [BsonElement("requesterId")]
    public string RequesterId { get; set; } = string.Empty; // Reference to User.Id

    [BsonElement("requesterInfo")]
    public OrderRequesterInfo RequesterInfo { get; set; } = new();

    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty; // Reference to Product.Id

    [BsonElement("productInfo")]
    public OrderProductInfo ProductInfo { get; set; } = new();

    [BsonElement("quantity")]
    public int Quantity { get; set; } = 1;

    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";

    [BsonElement("status")]
    public string Status { get; set; } = "draft"; // draft|submitted|under_review|approved|rejected|fulfilled|cancelled

    [BsonElement("priority")]
    public string Priority { get; set; } = "medium"; // low|medium|high|urgent

    [BsonElement("businessJustification")]
    public string BusinessJustification { get; set; } = string.Empty;

    [BsonElement("customResponses")]
    public List<OrderCustomResponse> CustomResponses { get; set; } = new();

    [BsonElement("approvalWorkflow")]
    public OrderApprovalWorkflow ApprovalWorkflow { get; set; } = new();

    [BsonElement("aiAgentSessionId")]
    public string? AiAgentSessionId { get; set; } // Reference to AIAgentSession.Id

    [BsonElement("deliveryInfo")]
    public OrderDeliveryInfo DeliveryInfo { get; set; } = new();

    [BsonElement("metadata")]
    public OrderMetadata Metadata { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("submittedAt")]
    public DateTime? SubmittedAt { get; set; }

    [BsonElement("requiredByDate")]
    public DateTime? RequiredByDate { get; set; }

    [BsonElement("fulfilledAt")]
    public DateTime? FulfilledAt { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Requester information snapshot for the order
/// </summary>
public class OrderRequesterInfo
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

    [BsonElement("managerId")]
    public string? ManagerId { get; set; }

    [BsonElement("managerName")]
    public string? ManagerName { get; set; }
}

/// <summary>
/// Product information snapshot for the order
/// </summary>
public class OrderProductInfo
{
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";

    [BsonElement("vendor")]
    public string Vendor { get; set; } = string.Empty;

    [BsonElement("sku")]
    public string Sku { get; set; } = string.Empty;
}

/// <summary>
/// Custom responses to product-specific questions
/// </summary>
public class OrderCustomResponse
{
    [BsonElement("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [BsonElement("questionKey")]
    public string QuestionKey { get; set; } = string.Empty;

    [BsonElement("question")]
    public string Question { get; set; } = string.Empty;

    [BsonElement("response")]
    public string Response { get; set; } = string.Empty;

    [BsonElement("responseType")]
    public string ResponseType { get; set; } = string.Empty; // text|number|select|multiselect|date

    [BsonElement("isRequired")]
    public bool IsRequired { get; set; }

    [BsonElement("answeredAt")]
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Complete approval workflow tracking for the order
/// </summary>
public class OrderApprovalWorkflow
{
    [BsonElement("isRequired")]
    public bool IsRequired { get; set; } = true;

    [BsonElement("currentStep")]
    public int CurrentStep { get; set; } = 0;

    [BsonElement("totalSteps")]
    public int TotalSteps { get; set; } = 0;

    [BsonElement("status")]
    public string Status { get; set; } = "pending"; // pending|in_progress|approved|rejected|bypassed

    [BsonElement("approvers")]
    public List<OrderApprover> Approvers { get; set; } = new();

    [BsonElement("history")]
    public List<OrderApprovalAction> History { get; set; } = new();

    [BsonElement("autoApprovalRules")]
    public List<string> AutoApprovalRules { get; set; } = new();

    [BsonElement("escalationTriggers")]
    public List<OrderEscalationTrigger> EscalationTriggers { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Individual approver in the workflow
/// </summary>
public class OrderApprover
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } = string.Empty;

    [BsonElement("department")]
    public string Department { get; set; } = string.Empty;

    [BsonElement("stepNumber")]
    public int StepNumber { get; set; }

    [BsonElement("approvalLimit")]
    public decimal ApprovalLimit { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = "pending"; // pending|approved|rejected|skipped|escalated

    [BsonElement("isRequired")]
    public bool IsRequired { get; set; } = true;

    [BsonElement("assignedAt")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("respondedAt")]
    public DateTime? RespondedAt { get; set; }

    [BsonElement("deadline")]
    public DateTime? Deadline { get; set; }

    [BsonElement("remindersSent")]
    public int RemindersSent { get; set; } = 0;
}

/// <summary>
/// Approval action history tracking
/// </summary>
public class OrderApprovalAction
{
    [BsonElement("actionId")]
    public string ActionId { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("userName")]
    public string UserName { get; set; } = string.Empty;

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty; // submit|approve|reject|request_info|escalate|cancel

    [BsonElement("stepNumber")]
    public int StepNumber { get; set; }

    [BsonElement("comments")]
    public string Comments { get; set; } = string.Empty;

    [BsonElement("reason")]
    public string Reason { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }

    [BsonElement("userAgent")]
    public string? UserAgent { get; set; }

    [BsonElement("additionalData")]
    public Dictionary<string, string> AdditionalData { get; set; } = new();
}

/// <summary>
/// Escalation triggers for approval workflow
/// </summary>
public class OrderEscalationTrigger
{
    [BsonElement("triggerId")]
    public string TriggerId { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("triggerType")]
    public string TriggerType { get; set; } = string.Empty; // timeout|amount_threshold|risk_factor|manual

    [BsonElement("condition")]
    public string Condition { get; set; } = string.Empty;

    [BsonElement("escalateTo")]
    public string EscalateTo { get; set; } = string.Empty; // User ID to escalate to

    [BsonElement("escalateToName")]
    public string EscalateToName { get; set; } = string.Empty;

    [BsonElement("isTriggered")]
    public bool IsTriggered { get; set; } = false;

    [BsonElement("triggeredAt")]
    public DateTime? TriggeredAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Delivery and fulfillment information
/// </summary>
public class OrderDeliveryInfo
{
    [BsonElement("deliveryMethod")]
    public string DeliveryMethod { get; set; } = "digital"; // digital|physical|service

    [BsonElement("deliveryAddress")]
    public OrderAddress? DeliveryAddress { get; set; }

    [BsonElement("expectedDeliveryDate")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [BsonElement("actualDeliveryDate")]
    public DateTime? ActualDeliveryDate { get; set; }

    [BsonElement("trackingNumber")]
    public string? TrackingNumber { get; set; }

    [BsonElement("deliveryStatus")]
    public string DeliveryStatus { get; set; } = "pending"; // pending|in_transit|delivered|failed

    [BsonElement("deliveryNotes")]
    public string DeliveryNotes { get; set; } = string.Empty;

    [BsonElement("specialInstructions")]
    public string SpecialInstructions { get; set; } = string.Empty;
}

/// <summary>
/// Delivery address information
/// </summary>
public class OrderAddress
{
    [BsonElement("recipientName")]
    public string RecipientName { get; set; } = string.Empty;

    [BsonElement("company")]
    public string Company { get; set; } = string.Empty;

    [BsonElement("addressLine1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [BsonElement("addressLine2")]
    public string? AddressLine2 { get; set; }

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("state")]
    public string State { get; set; } = string.Empty;

    [BsonElement("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    [BsonElement("country")]
    public string Country { get; set; } = "US";

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("email")]
    public string? Email { get; set; }
}

/// <summary>
/// Order metadata and tracking information
/// </summary>
public class OrderMetadata
{
    [BsonElement("source")]
    public string Source { get; set; } = "ai_agent"; // ai_agent|web_portal|api|import

    [BsonElement("sourceDetails")]
    public Dictionary<string, string> SourceDetails { get; set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("riskScore")]
    public decimal RiskScore { get; set; } = 0;

    [BsonElement("riskFactors")]
    public List<string> RiskFactors { get; set; } = new();

    [BsonElement("budgetCode")]
    public string? BudgetCode { get; set; }

    [BsonElement("projectCode")]
    public string? ProjectCode { get; set; }

    [BsonElement("costCenter")]
    public string? CostCenter { get; set; }

    [BsonElement("vendorOrderNumber")]
    public string? VendorOrderNumber { get; set; }

    [BsonElement("internalNotes")]
    public List<OrderNote> InternalNotes { get; set; } = new();

    [BsonElement("attachments")]
    public List<OrderAttachment> Attachments { get; set; } = new();

    [BsonElement("complianceChecks")]
    public List<OrderComplianceCheck> ComplianceChecks { get; set; } = new();
}

/// <summary>
/// Internal notes on the order
/// </summary>
public class OrderNote
{
    [BsonElement("noteId")]
    public string NoteId { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("authorId")]
    public string AuthorId { get; set; } = string.Empty;

    [BsonElement("authorName")]
    public string AuthorName { get; set; } = string.Empty;

    [BsonElement("note")]
    public string Note { get; set; } = string.Empty;

    [BsonElement("isPrivate")]
    public bool IsPrivate { get; set; } = false;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// File attachments for the order
/// </summary>
public class OrderAttachment
{
    [BsonElement("attachmentId")]
    public string AttachmentId { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    [BsonElement("fileSize")]
    public long FileSize { get; set; }

    [BsonElement("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    [BsonElement("uploadedBy")]
    public string UploadedBy { get; set; } = string.Empty;

    [BsonElement("uploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("storageUrl")]
    public string StorageUrl { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Compliance checks for the order
/// </summary>
public class OrderComplianceCheck
{
    [BsonElement("checkId")]
    public string CheckId { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("checkType")]
    public string CheckType { get; set; } = string.Empty; // budget|security|policy|vendor|legal

    [BsonElement("status")]
    public string Status { get; set; } = "pending"; // pending|passed|failed|waived

    [BsonElement("result")]
    public string Result { get; set; } = string.Empty;

    [BsonElement("details")]
    public string Details { get; set; } = string.Empty;

    [BsonElement("checkedBy")]
    public string CheckedBy { get; set; } = "system";

    [BsonElement("checkedAt")]
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("validUntil")]
    public DateTime? ValidUntil { get; set; }
}
