using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgenticOrderingSystem.API.Models;

/// <summary>
/// User management and approval authority tracking
/// Stored in CMP_DB.users collection
/// </summary>
public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("employeeId")]
    public string EmployeeId { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [BsonElement("lastName")]
    public string LastName { get; set; } = string.Empty;

    [BsonElement("department")]
    public string Department { get; set; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } = string.Empty; // employee|manager|director|md|admin

    [BsonElement("managerId")]
    public string? ManagerId { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    [BsonElement("approvalAuthority")]
    public ApprovalAuthority ApprovalAuthority { get; set; } = new();

    [BsonElement("preferences")]
    public UserPreferences Preferences { get; set; } = new();

    [BsonElement("contactInfo")]
    public ContactInfo ContactInfo { get; set; } = new();

    // Computed property for display
    [BsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class ApprovalAuthority
{
    [BsonElement("canApprove")]
    public bool CanApprove { get; set; }

    [BsonElement("maxApprovalAmount")]
    public decimal MaxApprovalAmount { get; set; }

    [BsonElement("departments")]
    public List<string> Departments { get; set; } = new();

    [BsonElement("productCategories")]
    public List<string> ProductCategories { get; set; } = new();

    [BsonElement("delegationRules")]
    public DelegationRules DelegationRules { get; set; } = new();
}

public class DelegationRules
{
    [BsonElement("canDelegate")]
    public bool CanDelegate { get; set; }

    [BsonElement("delegateToRoles")]
    public List<string> DelegateToRoles { get; set; } = new();

    [BsonElement("maxDelegationDays")]
    public int MaxDelegationDays { get; set; } = 30;
}

public class UserPreferences
{
    [BsonElement("notificationMethods")]
    public List<string> NotificationMethods { get; set; } = new() { "email", "inapp" };

    [BsonElement("language")]
    public string Language { get; set; } = "en-US";

    [BsonElement("timezone")]
    public string Timezone { get; set; } = "UTC";

    [BsonElement("dashboardLayout")]
    public Dictionary<string, object> DashboardLayout { get; set; } = new();
}

public class ContactInfo
{
    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("slackId")]
    public string SlackId { get; set; } = string.Empty;

    [BsonElement("officeLocation")]
    public string OfficeLocation { get; set; } = string.Empty;
}
