using AgenticOrderingSystem.API.Models;

namespace AgenticOrderingSystem.API.Data;

/// <summary>
/// Mock data generator for Users with realistic organizational hierarchy
/// </summary>
public static class UserMockData
{
    public static List<User> GetMockUsers()
    {
        return new List<User>
        {
            // MANAGING DIRECTOR
            new User
            {
                Id = "user_md_sarah",
                EmployeeId = "EMP001",
                Email = "sarah.wilson@company.com",
                FirstName = "Sarah",
                LastName = "Wilson",
                Department = "Executive",
                Role = "md",
                ManagerId = null, // No manager - top level
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-2),
                LastLoginAt = DateTime.UtcNow.AddHours(-2),
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = true,
                    MaxApprovalAmount = 1000000, // $1M limit
                    Departments = new List<string> { "Engineering", "Marketing", "Sales", "Operations", "HR", "Finance" },
                    ProductCategories = new List<string> { "software", "hardware", "training", "services", "travel" },
                    DelegationRules = new DelegationRules
                    {
                        CanDelegate = true,
                        DelegateToRoles = new List<string> { "director" },
                        MaxDelegationDays = 30
                    }
                },
                Preferences = new UserPreferences
                {
                    NotificationMethods = new List<string> { "email", "sms", "inapp" },
                    Language = "en-US",
                    Timezone = "America/New_York"
                },
                ContactInfo = new ContactInfo
                {
                    Phone = "+1-555-0101",
                    SlackId = "@sarah.wilson",
                    OfficeLocation = "New York HQ"
                }
            },

            // DIRECTORS
            new User
            {
                Id = "user_dir_james",
                EmployeeId = "EMP002",
                Email = "james.chen@company.com",
                FirstName = "James",
                LastName = "Chen",
                Department = "Engineering",
                Role = "director",
                ManagerId = "user_md_sarah",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1),
                LastLoginAt = DateTime.UtcNow.AddHours(-4),
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = true,
                    MaxApprovalAmount = 150000, // $150K limit
                    Departments = new List<string> { "Engineering", "IT" },
                    ProductCategories = new List<string> { "software", "hardware", "training", "services" },
                    DelegationRules = new DelegationRules
                    {
                        CanDelegate = true,
                        DelegateToRoles = new List<string> { "manager" },
                        MaxDelegationDays = 14
                    }
                },
                Preferences = new UserPreferences
                {
                    NotificationMethods = new List<string> { "email", "slack", "inapp" },
                    Language = "en-US",
                    Timezone = "America/Los_Angeles"
                },
                ContactInfo = new ContactInfo
                {
                    Phone = "+1-555-0102",
                    SlackId = "@james.chen",
                    OfficeLocation = "San Francisco Office"
                }
            },

            new User
            {
                Id = "user_dir_maria",
                EmployeeId = "EMP003",
                Email = "maria.garcia@company.com",
                FirstName = "Maria",
                LastName = "Garcia",
                Department = "Operations",
                Role = "director",
                ManagerId = "user_md_sarah",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-8),
                LastLoginAt = DateTime.UtcNow.AddHours(-1),
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = true,
                    MaxApprovalAmount = 100000, // $100K limit
                    Departments = new List<string> { "Operations", "HR", "Finance" },
                    ProductCategories = new List<string> { "software", "services", "training", "travel" },
                    DelegationRules = new DelegationRules
                    {
                        CanDelegate = true,
                        DelegateToRoles = new List<string> { "manager" },
                        MaxDelegationDays = 14
                    }
                },
                Preferences = new UserPreferences
                {
                    NotificationMethods = new List<string> { "email", "inapp" },
                    Language = "en-US",
                    Timezone = "America/Chicago"
                },
                ContactInfo = new ContactInfo
                {
                    Phone = "+1-555-0103",
                    SlackId = "@maria.garcia",
                    OfficeLocation = "Chicago Office"
                }
            },

            // MANAGERS
            new User
            {
                Id = "user_mgr_alice",
                EmployeeId = "EMP101",
                Email = "alice.manager@company.com",
                FirstName = "Alice",
                LastName = "Manager",
                Department = "Engineering",
                Role = "manager",
                ManagerId = "user_dir_james",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6),
                LastLoginAt = DateTime.UtcNow.AddMinutes(-30),
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = true,
                    MaxApprovalAmount = 25000, // $25K limit
                    Departments = new List<string> { "Engineering" },
                    ProductCategories = new List<string> { "software", "hardware", "training" },
                    DelegationRules = new DelegationRules
                    {
                        CanDelegate = false
                    }
                },
                Preferences = new UserPreferences
                {
                    NotificationMethods = new List<string> { "email", "slack", "inapp" },
                    Language = "en-US",
                    Timezone = "America/Los_Angeles"
                },
                ContactInfo = new ContactInfo
                {
                    Phone = "+1-555-0201",
                    SlackId = "@alice.manager",
                    OfficeLocation = "San Francisco Office"
                }
            },

            new User
            {
                Id = "user_mgr_bob",
                EmployeeId = "EMP102",
                Email = "bob.smith@company.com",
                FirstName = "Bob",
                LastName = "Smith",
                Department = "IT",
                Role = "manager",
                ManagerId = "user_dir_james",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                LastLoginAt = DateTime.UtcNow.AddHours(-6),
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = true,
                    MaxApprovalAmount = 15000, // $15K limit
                    Departments = new List<string> { "IT" },
                    ProductCategories = new List<string> { "software", "hardware", "services" },
                    DelegationRules = new DelegationRules
                    {
                        CanDelegate = false
                    }
                },
                Preferences = new UserPreferences
                {
                    NotificationMethods = new List<string> { "email", "inapp" },
                    Language = "en-US",
                    Timezone = "America/New_York"
                },
                ContactInfo = new ContactInfo
                {
                    Phone = "+1-555-0202",
                    SlackId = "@bob.smith",
                    OfficeLocation = "New York HQ"
                }
            },

            new User
            {
                Id = "user_mgr_carol",
                EmployeeId = "EMP103",
                Email = "carol.johnson@company.com",
                FirstName = "Carol",
                LastName = "Johnson",
                Department = "Operations",
                Role = "manager",
                ManagerId = "user_dir_maria",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-3),
                LastLoginAt = DateTime.UtcNow.AddHours(-8),
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = true,
                    MaxApprovalAmount = 10000, // $10K limit
                    Departments = new List<string> { "Operations" },
                    ProductCategories = new List<string> { "services", "training", "travel" },
                    DelegationRules = new DelegationRules
                    {
                        CanDelegate = false
                    }
                },
                Preferences = new UserPreferences
                {
                    NotificationMethods = new List<string> { "email", "inapp" },
                    Language = "en-US",
                    Timezone = "America/Chicago"
                },
                ContactInfo = new ContactInfo
                {
                    Phone = "+1-555-0203",
                    SlackId = "@carol.johnson",
                    OfficeLocation = "Chicago Office"
                }
            },

            // EMPLOYEES (Regular users who submit orders)
            new User
            {
                Id = "user_emp_john",
                EmployeeId = "EMP301",
                Email = "john.doe@company.com",
                FirstName = "John",
                LastName = "Doe",
                Department = "Engineering",
                Role = "employee",
                ManagerId = "user_mgr_alice",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-2),
                LastLoginAt = DateTime.UtcNow.AddMinutes(-15),
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = false,
                    MaxApprovalAmount = 0,
                    Departments = new List<string>(),
                    ProductCategories = new List<string>()
                },
                Preferences = new UserPreferences
                {
                    NotificationMethods = new List<string> { "email", "slack" },
                    Language = "en-US",
                    Timezone = "America/Los_Angeles"
                },
                ContactInfo = new ContactInfo
                {
                    Phone = "+1-555-0301",
                    SlackId = "@john.doe",
                    OfficeLocation = "San Francisco Office"
                }
            },

            new User
            {
                Id = "user_emp_jane",
                EmployeeId = "EMP302",
                Email = "jane.designer@company.com",
                FirstName = "Jane",
                LastName = "Designer",
                Department = "Marketing",
                Role = "employee",
                ManagerId = "user_mgr_alice", // Cross-department reporting
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-1),
                LastLoginAt = DateTime.UtcNow.AddHours(-2),
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = false,
                    MaxApprovalAmount = 0,
                    Departments = new List<string>(),
                    ProductCategories = new List<string>()
                },
                Preferences = new UserPreferences
                {
                    NotificationMethods = new List<string> { "email", "inapp" },
                    Language = "en-US",
                    Timezone = "America/New_York"
                },
                ContactInfo = new ContactInfo
                {
                    Phone = "+1-555-0302",
                    SlackId = "@jane.designer",
                    OfficeLocation = "New York HQ"
                }
            },

            new User
            {
                Id = "user_emp_mike",
                EmployeeId = "EMP303",
                Email = "mike.developer@company.com",
                FirstName = "Mike",
                LastName = "Developer",
                Department = "Engineering",
                Role = "employee",
                ManagerId = "user_mgr_alice",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-21), // 3 weeks ago
                LastLoginAt = DateTime.UtcNow.AddMinutes(-45),
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = false,
                    MaxApprovalAmount = 0,
                    Departments = new List<string>(),
                    ProductCategories = new List<string>()
                },
                Preferences = new UserPreferences
                {
                    NotificationMethods = new List<string> { "slack", "inapp" },
                    Language = "en-US",
                    Timezone = "America/Los_Angeles"
                },
                ContactInfo = new ContactInfo
                {
                    Phone = "+1-555-0303",
                    SlackId = "@mike.developer",
                    OfficeLocation = "San Francisco Office"
                }
            },

            new User
            {
                Id = "user_emp_lisa",
                EmployeeId = "EMP304",
                Email = "lisa.analyst@company.com",
                FirstName = "Lisa",
                LastName = "Analyst",
                Department = "Operations",
                Role = "employee",
                ManagerId = "user_mgr_carol",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-14), // 2 weeks ago
                LastLoginAt = DateTime.UtcNow.AddHours(-12),
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = false,
                    MaxApprovalAmount = 0,
                    Departments = new List<string>(),
                    ProductCategories = new List<string>()
                },
                Preferences = new UserPreferences
                {
                    NotificationMethods = new List<string> { "email" },
                    Language = "en-US",
                    Timezone = "America/Chicago"
                },
                ContactInfo = new ContactInfo
                {
                    Phone = "+1-555-0304",
                    SlackId = "@lisa.analyst",
                    OfficeLocation = "Chicago Office"
                }
            },

            // ADMIN USER for system administration
            new User
            {
                Id = "user_admin_system",
                EmployeeId = "ADMIN001",
                Email = "admin@company.com",
                FirstName = "System",
                LastName = "Administrator",
                Department = "IT",
                Role = "admin",
                ManagerId = "user_dir_james",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1),
                LastLoginAt = DateTime.UtcNow.AddMinutes(-5),
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = true,
                    MaxApprovalAmount = 50000, // $50K limit for admin tasks
                    Departments = new List<string> { "Engineering", "IT", "Operations" },
                    ProductCategories = new List<string> { "software", "hardware", "services" },
                    DelegationRules = new DelegationRules
                    {
                        CanDelegate = true,
                        DelegateToRoles = new List<string> { "manager", "director" },
                        MaxDelegationDays = 7
                    }
                },
                Preferences = new UserPreferences
                {
                    NotificationMethods = new List<string> { "email", "sms", "slack", "inapp" },
                    Language = "en-US",
                    Timezone = "UTC"
                },
                ContactInfo = new ContactInfo
                {
                    Phone = "+1-555-0001",
                    SlackId = "@admin",
                    OfficeLocation = "Data Center"
                }
            }
        };
    }
}
