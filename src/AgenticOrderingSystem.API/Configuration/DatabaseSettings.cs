namespace AgenticOrderingSystem.API.Configuration;

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseSettings
{
    public string ProductDesignerConnectionString { get; set; } = string.Empty;
    public string CMPConnectionString { get; set; } = string.Empty;
    public string ProductDesignerDatabaseName { get; set; } = "ProductDesigner_DB";
    public string CMPDatabaseName { get; set; } = "CMP_DB";
}

/// <summary>
/// Collection name constants
/// </summary>
public static class CollectionNames
{
    // ProductDesigner_DB collections
    public const string Products = "products";
    public const string Categories = "categories";

    // CMP_DB collections
    public const string Users = "users";
    public const string Orders = "orders";
    public const string ApprovalHistory = "approvalHistory";
    public const string AIAgentSessions = "aiAgentSessions";
    public const string Notifications = "notifications";
}
