using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgenticOrderingSystem.API.Models;

/// <summary>
/// Product definition with approval models and dynamic questions
/// Stored in ProductDesigner_DB.products collection
/// </summary>
public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty; // software|hardware|training|services

    [BsonElement("price")]
    public decimal Price { get; set; } = 0;

    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("approvalModel")]
    public ApprovalModel ApprovalModel { get; set; } = new();

    [BsonElement("questions")]
    public List<ProductQuestion> Questions { get; set; } = new();

    [BsonElement("metadata")]
    public ProductMetadata Metadata { get; set; } = new();
}

public class ApprovalModel
{
    [BsonElement("level1")]
    public ApprovalLevel Level1 { get; set; } = new();

    [BsonElement("level2")]
    public ApprovalLevel Level2 { get; set; } = new();
}

public class ApprovalLevel
{
    [BsonElement("required")]
    public bool Required { get; set; }

    [BsonElement("approverType")]
    public string ApproverType { get; set; } = string.Empty; // manager|director|md

    [BsonElement("timeoutHours")]
    public int TimeoutHours { get; set; } = 48;

    [BsonElement("triggerConditions")]
    public TriggerConditions TriggerConditions { get; set; } = new();
}

public class TriggerConditions
{
    [BsonElement("minAmount")]
    public decimal? MinAmount { get; set; }

    [BsonElement("maxAmount")]
    public decimal? MaxAmount { get; set; }

    [BsonElement("departments")]
    public List<string> Departments { get; set; } = new();

    [BsonElement("riskFactors")]
    public List<string> RiskFactors { get; set; } = new();

    [BsonElement("escalationRules")]
    public List<string> EscalationRules { get; set; } = new();
}

public class ProductQuestion
{
    [BsonElement("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [BsonElement("key")]
    public string Key { get; set; } = string.Empty;

    [BsonElement("question")]
    public string Question { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty; // text|textarea|number|select|multiselect|date

    [BsonElement("required")]
    public bool Required { get; set; }

    [BsonElement("validation")]
    public QuestionValidation Validation { get; set; } = new();

    [BsonElement("options")]
    public List<QuestionOption> Options { get; set; } = new();

    [BsonElement("helpText")]
    public string HelpText { get; set; } = string.Empty;

    [BsonElement("order")]
    public int Order { get; set; }

    [BsonElement("conditionalDisplay")]
    public ConditionalDisplay? ConditionalDisplay { get; set; }
}

public class QuestionValidation
{
    [BsonElement("pattern")]
    public string? Pattern { get; set; }

    [BsonElement("min")]
    public decimal? Min { get; set; }

    [BsonElement("max")]
    public decimal? Max { get; set; }

    [BsonElement("length")]
    public LengthValidation? Length { get; set; }
}

public class LengthValidation
{
    [BsonElement("min")]
    public int Min { get; set; }

    [BsonElement("max")]
    public int Max { get; set; }
}

public class QuestionOption
{
    [BsonElement("value")]
    public string Value { get; set; } = string.Empty;

    [BsonElement("label")]
    public string Label { get; set; } = string.Empty;
}

public class ConditionalDisplay
{
    [BsonElement("dependsOn")]
    public string DependsOn { get; set; } = string.Empty;

    [BsonElement("condition")]
    public string Condition { get; set; } = string.Empty; // equals|contains|greaterThan

    [BsonElement("value")]
    public string Value { get; set; } = string.Empty;
}

public class ProductMetadata
{
    [BsonElement("vendor")]
    public string Vendor { get; set; } = string.Empty;

    [BsonElement("businessUnit")]
    public string BusinessUnit { get; set; } = string.Empty;

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("estimatedCost")]
    public decimal? EstimatedCost { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";
}
