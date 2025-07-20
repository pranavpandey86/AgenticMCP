using AgenticOrderingSystem.API.Models;
using AgenticOrderingSystem.API.Services;
using MongoDB.Driver;

namespace AgenticOrderingSystem.API.Data;

/// <summary>
/// Seeds team-based order data with realistic failure patterns and success examples
/// </summary>
public class TeamBasedOrderSeeder
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<TeamBasedOrderSeeder> _logger;

    public TeamBasedOrderSeeder(IDatabaseService databaseService, ILogger<TeamBasedOrderSeeder> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task SeedTeamBasedOrdersAsync()
    {
        try
        {
            _logger.LogInformation("Starting team-based order seeding...");

            // First, create team structure with managers and team members
            await SeedTeamStructureAsync();
            
            // Then create realistic order scenarios
            await SeedTeamOrderPatternsAsync();

            _logger.LogInformation("Team-based order seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during team-based order seeding");
            throw;
        }
    }

    private async Task SeedTeamStructureAsync()
    {
        var userCollection = _databaseService.Users;

        // Clear existing users first
        await userCollection.DeleteManyAsync(_ => true);

        var users = new List<User>
        {
            // Managers
            new User
            {
                Id = "mgr_eng_sarah",
                EmployeeId = "EMP001",
                Email = "sarah.wilson@company.com",
                FirstName = "Sarah",
                LastName = "Wilson",
                Department = "Engineering",
                Role = "manager",
                IsActive = true,
                ManagerId = null, // Top level manager
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = true,
                    MaxApprovalAmount = 5000,
                    Departments = new List<string> { "Engineering" }
                }
            },
            new User
            {
                Id = "mgr_mkt_jennifer",
                EmployeeId = "EMP002",
                Email = "jennifer.davis@company.com",
                FirstName = "Jennifer",
                LastName = "Davis",
                Department = "Marketing",
                Role = "manager",
                IsActive = true,
                ManagerId = null, // Top level manager
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = true,
                    MaxApprovalAmount = 3000,
                    Departments = new List<string> { "Marketing" }
                }
            },

            // Engineering Team Members
            new User
            {
                Id = "eng_mike_backend",
                EmployeeId = "EMP003",
                Email = "mike.chen@company.com",
                FirstName = "Mike",
                LastName = "Chen",
                Department = "Engineering",
                Role = "employee",
                IsActive = true,
                ManagerId = "mgr_eng_sarah",
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = false,
                    MaxApprovalAmount = 0
                }
            },
            new User
            {
                Id = "eng_anna_frontend",
                EmployeeId = "EMP004",
                Email = "anna.peterson@company.com",
                FirstName = "Anna",
                LastName = "Peterson",
                Department = "Engineering",
                Role = "employee",
                IsActive = true,
                ManagerId = "mgr_eng_sarah",
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = false,
                    MaxApprovalAmount = 0
                }
            },

            // Marketing Team Members
            new User
            {
                Id = "mkt_david_designer",
                EmployeeId = "EMP005",
                Email = "david.rodriguez@company.com",
                FirstName = "David",
                LastName = "Rodriguez",
                Department = "Marketing",
                Role = "employee",
                IsActive = true,
                ManagerId = "mgr_mkt_jennifer",
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = false,
                    MaxApprovalAmount = 0
                }
            },
            new User
            {
                Id = "mkt_lisa_content",
                EmployeeId = "EMP006",
                Email = "lisa.johnson@company.com",
                FirstName = "Lisa",
                LastName = "Johnson",
                Department = "Marketing",
                Role = "employee",
                IsActive = true,
                ManagerId = "mgr_mkt_jennifer",
                ApprovalAuthority = new ApprovalAuthority
                {
                    CanApprove = false,
                    MaxApprovalAmount = 0
                }
            }
        };

        await userCollection.InsertManyAsync(users);
        _logger.LogInformation("Seeded {Count} team members", users.Count);
    }

    private async Task SeedTeamOrderPatternsAsync()
    {
        var orders = _databaseService.Orders;

        // Clear existing orders first
        await orders.DeleteManyAsync(_ => true);

        var baseDate = DateTime.UtcNow.AddDays(-30);
        var teamOrders = new List<Order>();

        // Add version conflict scenarios
        teamOrders.AddRange(CreateVersionConflictScenario(baseDate));
        
        // Add team success examples
        teamOrders.AddRange(CreateTeamSuccessScenario(baseDate.AddDays(5)));

        await orders.InsertManyAsync(teamOrders);
        _logger.LogInformation("Seeded {Count} team-based orders", teamOrders.Count);
    }

    private List<Order> CreateVersionConflictScenario(DateTime baseDate)
    {
        return new List<Order>
        {
            // David from marketing tries older Adobe CC version - FAILS
            new Order
            {
                Id = $"order_{Guid.NewGuid()}",
                OrderNumber = "TEAM-FAIL-001",
                RequesterId = "mkt_david_designer",
                RequesterInfo = new OrderRequesterInfo
                {
                    UserId = "mkt_david_designer",
                    FullName = "David Rodriguez",
                    Email = "david.rodriguez@company.com",
                    Department = "Marketing",
                    Role = "employee",
                    ManagerId = "mgr_mkt_jennifer",
                    ManagerName = "Jennifer Davis"
                },
                ProductId = "adobe-cc-2023",
                ProductInfo = new OrderProductInfo
                {
                    Name = "Adobe Creative Cloud 2023",
                    Category = "software",
                    UnitPrice = 45.99m,
                    Vendor = "Adobe"
                },
                Quantity = 1,
                TotalAmount = 45.99m,
                Status = "rejected",
                BusinessJustification = "Need Adobe Creative Cloud for marketing materials",
                CreatedAt = baseDate,
                ApprovalWorkflow = new OrderApprovalWorkflow
                {
                    CurrentStep = 1,
                    Status = "rejected",
                    History = new List<OrderApprovalAction>
                    {
                        new OrderApprovalAction
                        {
                            StepNumber = 1,
                            UserId = "mgr_mkt_jennifer",
                            Action = "reject",
                            Comments = "🚫 REJECTED: Adobe Creative Cloud 2023 is discontinued. Current available version is 2024.1.0. Please reorder with the latest version for continued support and security updates.",
                            Reason = "Outdated version requested",
                            Timestamp = baseDate.AddHours(2)
                        }
                    }
                },
                Metadata = new OrderMetadata
                {
                    InternalNotes = new List<OrderNote>
                    {
                        new OrderNote
                        {
                            AuthorId = "system",
                            AuthorName = "System",
                            Note = "Version conflict detected: Adobe Creative Cloud 2023 is no longer available",
                            CreatedAt = baseDate
                        }
                    }
                }
            },

            // Mike from engineering tries same Adobe CC but succeeds with newer version
            new Order
            {
                Id = $"order_{Guid.NewGuid()}",
                OrderNumber = "TEAM-SUCCESS-001",
                RequesterId = "eng_mike_backend",
                RequesterInfo = new OrderRequesterInfo
                {
                    UserId = "eng_mike_backend",
                    FullName = "Mike Chen",
                    Email = "mike.chen@company.com",
                    Department = "Engineering",
                    Role = "employee",
                    ManagerId = "mgr_eng_sarah",
                    ManagerName = "Sarah Wilson"
                },
                ProductId = "adobe-cc-2024",
                ProductInfo = new OrderProductInfo
                {
                    Name = "Adobe Creative Cloud 2024",
                    Category = "software",
                    UnitPrice = 52.99m,
                    Vendor = "Adobe"
                },
                Quantity = 1,
                TotalAmount = 52.99m,
                Status = "approved",
                BusinessJustification = "Need Adobe Creative Cloud 2024 for creating technical documentation, UI mockups, and presentation materials for engineering projects.",
                CreatedAt = baseDate.AddDays(1),
                ApprovalWorkflow = new OrderApprovalWorkflow
                {
                    CurrentStep = 1,
                    Status = "approved",
                    CompletedAt = baseDate.AddDays(1).AddHours(1),
                    History = new List<OrderApprovalAction>
                    {
                        new OrderApprovalAction
                        {
                            StepNumber = 1,
                            UserId = "mgr_eng_sarah",
                            Action = "approve",
                            Comments = "✅ APPROVED: Current version with full support. Good business justification provided.",
                            Timestamp = baseDate.AddDays(1).AddHours(1)
                        }
                    }
                }
            }
        };
    }

    private List<Order> CreateTeamSuccessScenario(DateTime baseDate)
    {
        return new List<Order>
        {
            // Lisa gets Adobe CC for marketing - SUCCEEDS (good team example)
            new Order
            {
                Id = $"order_{Guid.NewGuid()}",
                OrderNumber = "TEAM-SUCCESS-002",
                RequesterId = "mkt_lisa_content",
                RequesterInfo = new OrderRequesterInfo
                {
                    UserId = "mkt_lisa_content",
                    FullName = "Lisa Johnson",
                    Email = "lisa.johnson@company.com",
                    Department = "Marketing",
                    Role = "employee",
                    ManagerId = "mgr_mkt_jennifer",
                    ManagerName = "Jennifer Davis"
                },
                ProductId = "adobe-cc-2024",
                ProductInfo = new OrderProductInfo
                {
                    Name = "Adobe Creative Cloud 2024",
                    Category = "software",
                    UnitPrice = 52.99m,
                    Vendor = "Adobe"
                },
                Quantity = 1,
                TotalAmount = 52.99m,
                Status = "approved",
                BusinessJustification = "Need Adobe Creative Cloud for content creation including blog graphics, social media visuals, email campaign designs, and promotional materials. Will use Photoshop, Illustrator, and InDesign for marketing campaigns targeting Q4 product launches.",
                CreatedAt = baseDate,
                ApprovalWorkflow = new OrderApprovalWorkflow
                {
                    CurrentStep = 1,
                    Status = "approved",
                    CompletedAt = baseDate.AddHours(2),
                    History = new List<OrderApprovalAction>
                    {
                        new OrderApprovalAction
                        {
                            StepNumber = 1,
                            UserId = "mgr_mkt_jennifer",
                            Action = "approve",
                            Comments = "✅ APPROVED: Perfect tool for marketing content creation with detailed usage plan. Approved - excellent fit for marketing needs",
                            Timestamp = baseDate.AddHours(2)
                        }
                    }
                }
            }
        };
    }
}
