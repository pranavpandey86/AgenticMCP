using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgenticOrderingSystem.API.Models;

/// <summary>
/// Product categories for organization
/// Stored in ProductDesigner_DB.categories collection
/// </summary>
public class Category
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("parentCategoryId")]
    public string? ParentCategoryId { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }

    [BsonElement("metadata")]
    public CategoryMetadata Metadata { get; set; } = new();
}

public class CategoryMetadata
{
    [BsonElement("defaultApprovalModel")]
    public ApprovalModel? DefaultApprovalModel { get; set; }

    [BsonElement("requiredFields")]
    public List<string> RequiredFields { get; set; } = new();

    [BsonElement("businessRules")]
    public List<string> BusinessRules { get; set; } = new();

    [BsonElement("icon")]
    public string Icon { get; set; } = string.Empty;

    [BsonElement("color")]
    public string Color { get; set; } = string.Empty;
}
