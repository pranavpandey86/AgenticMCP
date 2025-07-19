using AgenticOrderingSystem.API.Data;
using AgenticOrderingSystem.API.Services;
using AgenticOrderingSystem.API.Models;
using MongoDB.Driver;

namespace AgenticOrderingSystem.API.Services;

/// <summary>
/// Service for seeding the database with mock data
/// </summary>
public interface IDataSeedingService
{
    Task SeedAllDataAsync();
    Task SeedCategoriesAsync();
    Task SeedProductsAsync();
    Task SeedUsersAsync();
    Task SeedRejectedOrdersAsync();
    Task SeedScenarioBasedOrdersAsync();
    Task ClearAllDataAsync();
    Task<DatabaseSeedStatus> GetSeedStatusAsync();
}

public class DataSeedingService : IDataSeedingService
{
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<DataSeedingService> _logger;

    public DataSeedingService(IDatabaseService databaseService, ILogger<DataSeedingService> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    public async Task SeedAllDataAsync()
    {
        _logger.LogInformation("🌱 Starting database seeding process...");

        try
        {
            // Clear existing data first
            await ClearAllDataAsync();

            // Seed in order (categories first, then products that reference them, then users)
            await SeedCategoriesAsync();
            await SeedProductsAsync();
            await SeedUsersAsync();

            _logger.LogInformation("✅ Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during database seeding");
            throw;
        }
    }

    public async Task SeedCategoriesAsync()
    {
        _logger.LogInformation("📁 Seeding categories...");

        var categories = CategoryMockData.GetMockCategories();
        await _databaseService.Categories.InsertManyAsync(categories);

        _logger.LogInformation($"✅ Seeded {categories.Count} categories");
    }

    public async Task SeedProductsAsync()
    {
        _logger.LogInformation("📦 Seeding products...");

        var products = ProductMockData.GetMockProducts();
        await _databaseService.Products.InsertManyAsync(products);

        _logger.LogInformation($"✅ Seeded {products.Count} products");
    }

    public async Task SeedUsersAsync()
    {
        _logger.LogInformation("👥 Seeding users...");

        var users = UserMockData.GetMockUsers();
        await _databaseService.Users.InsertManyAsync(users);

        _logger.LogInformation($"✅ Seeded {users.Count} users");
    }

    public async Task SeedRejectedOrdersAsync()
    {
        _logger.LogInformation("🚫 Seeding rejected orders...");

        // Get some sample users and products for rejected orders
        var john = await _databaseService.Users.Find(u => u.Id == "user_emp_john").FirstOrDefaultAsync();
        var jane = await _databaseService.Users.Find(u => u.Id == "user_emp_jane").FirstOrDefaultAsync();
        var lisa = await _databaseService.Users.Find(u => u.Id == "user_emp_lisa").FirstOrDefaultAsync();
        
        var securityConsulting = await _databaseService.Products.Find(p => p.Id == "prod_security_consulting").FirstOrDefaultAsync();
        var laptopDell = await _databaseService.Products.Find(p => p.Id == "prod_laptop_dell").FirstOrDefaultAsync();
        var awsTraining = await _databaseService.Products.Find(p => p.Id == "prod_aws_certification").FirstOrDefaultAsync();

        if (john == null || jane == null || lisa == null || securityConsulting == null || laptopDell == null || awsTraining == null)
        {
            _logger.LogWarning("❌ Required users or products not found for seeding rejected orders");
            return;
        }

        var rejectedOrders = new List<Order>
        {
            // Order 1: Security Consulting - Rejected by Alice Manager for budget constraints
            new Order
            {
                Id = Guid.NewGuid().ToString(),
                OrderNumber = "REJ-2025-07-0001",
                RequesterId = john.Id,
                RequesterInfo = new OrderRequesterInfo
                {
                    UserId = john.Id,
                    FullName = $"{john.FirstName} {john.LastName}",
                    Email = john.Email,
                    Department = john.Department,
                    Role = john.Role,
                    ManagerId = john.ManagerId
                },
                ProductId = securityConsulting.Id,
                ProductInfo = new OrderProductInfo
                {
                    ProductId = securityConsulting.Id,
                    Name = securityConsulting.Name,
                    Category = securityConsulting.Category,
                    UnitPrice = securityConsulting.Price,
                    Currency = "USD",
                    Vendor = securityConsulting.Metadata?.Vendor ?? "CyberSec Solutions"
                },
                Quantity = 2,
                TotalAmount = securityConsulting.Price * 2,
                Currency = "USD",
                Status = "rejected",
                Priority = "high",
                BusinessJustification = "Urgent security compliance audit required for Q3",
                ApprovalWorkflow = new OrderApprovalWorkflow
                {
                    IsRequired = true,
                    CurrentStep = 1,
                    TotalSteps = 3,
                    Status = "rejected",
                    Approvers = new List<OrderApprover>
                    {
                        new OrderApprover
                        {
                            UserId = "user_mgr_alice",
                            FullName = "Alice Manager",
                            Role = "manager",
                            Department = "Engineering",
                            StepNumber = 1,
                            ApprovalLimit = 25000,
                            Status = "rejected",
                            AssignedAt = DateTime.UtcNow.AddDays(-1).AddHours(-1),
                            RespondedAt = DateTime.UtcNow.AddDays(-1)
                        }
                    },
                    History = new List<OrderApprovalAction>
                    {
                        new OrderApprovalAction
                        {
                            ActionId = Guid.NewGuid().ToString(),
                            UserId = john.Id,
                            UserName = $"{john.FirstName} {john.LastName}",
                            Action = "submit",
                            StepNumber = 0,
                            Comments = "Order submitted for approval",
                            Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-2)
                        },
                        new OrderApprovalAction
                        {
                            ActionId = Guid.NewGuid().ToString(),
                            UserId = "user_mgr_alice",
                            UserName = "Alice Manager",
                            Action = "reject",
                            StepNumber = 1,
                            Comments = "Security consulting costs ($40,000) exceed quarterly budget allocation. Need to defer until next quarter and explore more cost-effective alternatives.",
                            Reason = "budget_constraints",
                            Timestamp = DateTime.UtcNow.AddDays(-1)
                        }
                    },
                    CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-2),
                    CompletedAt = DateTime.UtcNow.AddDays(-1)
                },
                CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                SubmittedAt = DateTime.UtcNow.AddDays(-1).AddHours(-2),
                IsActive = true
            },

            // Order 2: Dell Laptops - Rejected by Alice Manager for alternative options
            new Order
            {
                Id = Guid.NewGuid().ToString(),
                OrderNumber = "REJ-2025-07-0002",
                RequesterId = jane.Id,
                RequesterInfo = new OrderRequesterInfo
                {
                    UserId = jane.Id,
                    FullName = $"{jane.FirstName} {jane.LastName}",
                    Email = jane.Email,
                    Department = jane.Department,
                    Role = jane.Role,
                    ManagerId = jane.ManagerId
                },
                ProductId = laptopDell.Id,
                ProductInfo = new OrderProductInfo
                {
                    ProductId = laptopDell.Id,
                    Name = laptopDell.Name,
                    Category = laptopDell.Category,
                    UnitPrice = laptopDell.Price,
                    Currency = "USD",
                    Vendor = laptopDell.Metadata?.Vendor ?? "Dell Technologies"
                },
                Quantity = 5,
                TotalAmount = laptopDell.Price * 5,
                Currency = "USD",
                Status = "rejected",
                Priority = "medium",
                BusinessJustification = "Team expansion requires new laptops for developers",
                ApprovalWorkflow = new OrderApprovalWorkflow
                {
                    IsRequired = true,
                    CurrentStep = 1,
                    TotalSteps = 2,
                    Status = "rejected",
                    Approvers = new List<OrderApprover>
                    {
                        new OrderApprover
                        {
                            UserId = "user_mgr_alice",
                            FullName = "Alice Manager",
                            Role = "manager",
                            Department = "Engineering",
                            StepNumber = 1,
                            ApprovalLimit = 25000,
                            Status = "rejected",
                            AssignedAt = DateTime.UtcNow.AddHours(-5),
                            RespondedAt = DateTime.UtcNow.AddHours(-4)
                        }
                    },
                    History = new List<OrderApprovalAction>
                    {
                        new OrderApprovalAction
                        {
                            ActionId = Guid.NewGuid().ToString(),
                            UserId = jane.Id,
                            UserName = $"{jane.FirstName} {jane.LastName}",
                            Action = "submit",
                            StepNumber = 0,
                            Comments = "Order submitted for approval",
                            Timestamp = DateTime.UtcNow.AddHours(-5)
                        },
                        new OrderApprovalAction
                        {
                            ActionId = Guid.NewGuid().ToString(),
                            UserId = "user_mgr_alice",
                            UserName = "Alice Manager",
                            Action = "reject",
                            StepNumber = 1,
                            Comments = "Consider refurbished laptops or lease options to reduce upfront costs. Dell XPS models are expensive - explore alternatives.",
                            Reason = "cost_optimization",
                            Timestamp = DateTime.UtcNow.AddHours(-4)
                        }
                    },
                    CreatedAt = DateTime.UtcNow.AddHours(-5),
                    CompletedAt = DateTime.UtcNow.AddHours(-4)
                },
                CreatedAt = DateTime.UtcNow.AddHours(-5),
                UpdatedAt = DateTime.UtcNow.AddHours(-4),
                SubmittedAt = DateTime.UtcNow.AddHours(-5),
                IsActive = true
            },

            // Order 3: AWS Training - Rejected by Carol Johnson for timing
            new Order
            {
                Id = Guid.NewGuid().ToString(),
                OrderNumber = "REJ-2025-07-0003",
                RequesterId = lisa.Id,
                RequesterInfo = new OrderRequesterInfo
                {
                    UserId = lisa.Id,
                    FullName = $"{lisa.FirstName} {lisa.LastName}",
                    Email = lisa.Email,
                    Department = lisa.Department,
                    Role = lisa.Role,
                    ManagerId = lisa.ManagerId
                },
                ProductId = awsTraining.Id,
                ProductInfo = new OrderProductInfo
                {
                    ProductId = awsTraining.Id,
                    Name = awsTraining.Name,
                    Category = awsTraining.Category,
                    UnitPrice = awsTraining.Price,
                    Currency = "USD",
                    Vendor = awsTraining.Metadata?.Vendor ?? "AWS Training Center"
                },
                Quantity = 3,
                TotalAmount = awsTraining.Price * 3,
                Currency = "USD",
                Status = "rejected",
                Priority = "low",
                BusinessJustification = "Team needs AWS certification for cloud migration project",
                ApprovalWorkflow = new OrderApprovalWorkflow
                {
                    IsRequired = true,
                    CurrentStep = 1,
                    TotalSteps = 2,
                    Status = "rejected",
                    Approvers = new List<OrderApprover>
                    {
                        new OrderApprover
                        {
                            UserId = "user_mgr_carol",
                            FullName = "Carol Johnson",
                            Role = "manager",
                            Department = "Operations",
                            StepNumber = 1,
                            ApprovalLimit = 10000,
                            Status = "rejected",
                            AssignedAt = DateTime.UtcNow.AddHours(-3),
                            RespondedAt = DateTime.UtcNow.AddHours(-2)
                        }
                    },
                    History = new List<OrderApprovalAction>
                    {
                        new OrderApprovalAction
                        {
                            ActionId = Guid.NewGuid().ToString(),
                            UserId = lisa.Id,
                            UserName = $"{lisa.FirstName} {lisa.LastName}",
                            Action = "submit",
                            StepNumber = 0,
                            Comments = "Order submitted for approval",
                            Timestamp = DateTime.UtcNow.AddHours(-3)
                        },
                        new OrderApprovalAction
                        {
                            ActionId = Guid.NewGuid().ToString(),
                            UserId = "user_mgr_carol",
                            UserName = "Carol Johnson",
                            Action = "reject",
                            StepNumber = 1,
                            Comments = "Training budget is frozen until Q4. Also, prioritize internal cloud training before external certifications.",
                            Reason = "timing_issues",
                            Timestamp = DateTime.UtcNow.AddHours(-2)
                        }
                    },
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                    CompletedAt = DateTime.UtcNow.AddHours(-2)
                },
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                UpdatedAt = DateTime.UtcNow.AddHours(-2),
                SubmittedAt = DateTime.UtcNow.AddHours(-3),
                IsActive = true
            }
        };

        await _databaseService.Orders.InsertManyAsync(rejectedOrders);
        _logger.LogInformation($"✅ Seeded {rejectedOrders.Count} rejected orders");
    }

    public async Task SeedScenarioBasedOrdersAsync()
    {
        _logger.LogInformation("🎯 Seeding scenario-based orders for AI training...");

        // Get users and products for the scenarios
        var users = await _databaseService.Users.Find(u => u.IsActive).ToListAsync();
        var adobeProduct = await _databaseService.Products.Find(p => p.Id == "prod_adobe_creative").FirstOrDefaultAsync();

        if (adobeProduct == null || users.Count < 6)
        {
            _logger.LogWarning("❌ Required products or users not found for scenario-based orders");
            return;
        }

        var scenarioOrders = new List<Order>();

        // SCENARIO: Adobe Creative Suite - Same Product, Different Justifications
        // =====================================================================

        // ✅ APPROVED: Detailed business justification
        scenarioOrders.Add(new Order
        {
            Id = Guid.NewGuid().ToString(),
            OrderNumber = "SCEN-ADOBE-APPROVED-001",
            RequesterId = users[6].Id, // John Doe
            RequesterInfo = new OrderRequesterInfo
            {
                UserId = users[6].Id,
                FullName = $"{users[6].FirstName} {users[6].LastName}",
                Email = users[6].Email,
                Department = users[6].Department,
                Role = users[6].Role,
                ManagerId = users[6].ManagerId
            },
            ProductId = adobeProduct.Id,
            ProductInfo = new OrderProductInfo
            {
                ProductId = adobeProduct.Id,
                Name = adobeProduct.Name,
                Category = adobeProduct.Category,
                UnitPrice = adobeProduct.Price,
                Currency = "USD",
                Vendor = "Adobe Systems"
            },
            Quantity = 1,
            TotalAmount = adobeProduct.Price,
            Currency = "USD",
            Status = "approved",
            Priority = "medium",
            BusinessJustification = "Creating marketing materials for Q3 product launch campaign. Need Photoshop for image editing, Illustrator for logos, and InDesign for brochures. Expected to increase conversion rates by 15% based on previous campaigns with detailed ROI analysis.",
            ApprovalWorkflow = new OrderApprovalWorkflow
            {
                IsRequired = true,
                CurrentStep = 1,
                TotalSteps = 1,
                Status = "completed",
                Approvers = new List<OrderApprover>
                {
                    new OrderApprover
                    {
                        UserId = "user_mgr_alice",
                        FullName = "Alice Manager",
                        Role = "manager",
                        Department = "Engineering",
                        StepNumber = 1,
                        ApprovalLimit = 25000,
                        Status = "approved",
                        AssignedAt = DateTime.UtcNow.AddDays(-3),
                        RespondedAt = DateTime.UtcNow.AddDays(-3).AddHours(2)
                    }
                },
                History = new List<OrderApprovalAction>
                {
                    new OrderApprovalAction
                    {
                        ActionId = Guid.NewGuid().ToString(),
                        UserId = users[6].Id,
                        UserName = $"{users[6].FirstName} {users[6].LastName}",
                        Action = "submit",
                        StepNumber = 0,
                        Comments = "Order submitted with detailed business case",
                        Timestamp = DateTime.UtcNow.AddDays(-3).AddHours(-1)
                    },
                    new OrderApprovalAction
                    {
                        ActionId = Guid.NewGuid().ToString(),
                        UserId = "user_mgr_alice",
                        UserName = "Alice Manager",
                        Action = "approve",
                        StepNumber = 1,
                        Comments = "✅ APPROVED: Excellent business justification with clear ROI projections, specific use cases, and detailed project timeline. This demonstrates how to properly justify software purchases.",
                        Reason = "detailed_business_case",
                        Timestamp = DateTime.UtcNow.AddDays(-3).AddHours(2)
                    }
                },
                CreatedAt = DateTime.UtcNow.AddDays(-3).AddHours(-1),
                CompletedAt = DateTime.UtcNow.AddDays(-3).AddHours(2)
            },
            CreatedAt = DateTime.UtcNow.AddDays(-3).AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-3).AddHours(2),
            SubmittedAt = DateTime.UtcNow.AddDays(-3).AddHours(-1),
            IsActive = true
        });

        // 🚫 REJECTED: Vague business justification (same product)
        scenarioOrders.Add(new Order
        {
            Id = Guid.NewGuid().ToString(),
            OrderNumber = "SCEN-ADOBE-REJECTED-001", 
            RequesterId = users[7].Id, // Jane Designer
            RequesterInfo = new OrderRequesterInfo
            {
                UserId = users[7].Id,
                FullName = $"{users[7].FirstName} {users[7].LastName}",
                Email = users[7].Email,
                Department = users[7].Department,
                Role = users[7].Role,
                ManagerId = users[7].ManagerId
            },
            ProductId = adobeProduct.Id, // Same product as approved order
            ProductInfo = new OrderProductInfo
            {
                ProductId = adobeProduct.Id,
                Name = adobeProduct.Name,
                Category = adobeProduct.Category,
                UnitPrice = adobeProduct.Price,
                Currency = "USD",
                Vendor = "Adobe Systems"
            },
            Quantity = 1,
            TotalAmount = adobeProduct.Price,
            Currency = "USD",
            Status = "rejected",
            Priority = "high",
            BusinessJustification = "Need Adobe software for work stuff. Will be useful for projects and design work.",
            ApprovalWorkflow = new OrderApprovalWorkflow
            {
                IsRequired = true,
                CurrentStep = 1,
                TotalSteps = 1,
                Status = "rejected",
                Approvers = new List<OrderApprover>
                {
                    new OrderApprover
                    {
                        UserId = "user_mgr_alice",
                        FullName = "Alice Manager",
                        Role = "manager",
                        Department = "Engineering",
                        StepNumber = 1,
                        ApprovalLimit = 25000,
                        Status = "rejected",
                        AssignedAt = DateTime.UtcNow.AddDays(-2),
                        RespondedAt = DateTime.UtcNow.AddDays(-2).AddHours(1)
                    }
                },
                History = new List<OrderApprovalAction>
                {
                    new OrderApprovalAction
                    {
                        ActionId = Guid.NewGuid().ToString(),
                        UserId = users[7].Id,
                        UserName = $"{users[7].FirstName} {users[7].LastName}",
                        Action = "submit",
                        StepNumber = 0,
                        Comments = "Order submitted",
                        Timestamp = DateTime.UtcNow.AddDays(-2).AddHours(-1)
                    },
                    new OrderApprovalAction
                    {
                        ActionId = Guid.NewGuid().ToString(),
                        UserId = "user_mgr_alice",
                        UserName = "Alice Manager",
                        Action = "reject",
                        StepNumber = 1,
                        Comments = "🚫 REJECTED: Business justification is too vague. 'Work stuff' and 'design work' are not sufficient. Please provide: 1) Specific projects/campaigns, 2) Expected business value/ROI, 3) Timeline for usage, 4) Which Adobe tools are needed and why. See order SCEN-ADOBE-APPROVED-001 for a good example.",
                        Reason = "insufficient_business_justification",
                        Timestamp = DateTime.UtcNow.AddDays(-2).AddHours(1)
                    }
                },
                CreatedAt = DateTime.UtcNow.AddDays(-2).AddHours(-1),
                CompletedAt = DateTime.UtcNow.AddDays(-2).AddHours(1)
            },
            CreatedAt = DateTime.UtcNow.AddDays(-2).AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-2).AddHours(1),
            SubmittedAt = DateTime.UtcNow.AddDays(-2).AddHours(-1),
            IsActive = true
        });

        // ✅ APPROVED: Another good example (same product, different use case)
        scenarioOrders.Add(new Order
        {
            Id = Guid.NewGuid().ToString(),
            OrderNumber = "SCEN-ADOBE-APPROVED-002",
            RequesterId = users[8].Id, // Mike Developer
            RequesterInfo = new OrderRequesterInfo
            {
                UserId = users[8].Id,
                FullName = $"{users[8].FirstName} {users[8].LastName}",
                Email = users[8].Email,
                Department = users[8].Department,
                Role = users[8].Role,
                ManagerId = users[8].ManagerId
            },
            ProductId = adobeProduct.Id, // Same product
            ProductInfo = new OrderProductInfo
            {
                ProductId = adobeProduct.Id,
                Name = adobeProduct.Name,
                Category = adobeProduct.Category,
                UnitPrice = adobeProduct.Price,
                Currency = "USD",
                Vendor = "Adobe Systems"
            },
            Quantity = 1,
            TotalAmount = adobeProduct.Price,
            Currency = "USD",
            Status = "approved",
            Priority = "medium",
            BusinessJustification = "UI/UX design for new customer portal application. Need Photoshop for mockups, Illustrator for icons and graphics, XD for prototyping. Project budget $50K, expected to reduce customer support tickets by 30% and improve user satisfaction scores.",
            ApprovalWorkflow = new OrderApprovalWorkflow
            {
                IsRequired = true,
                CurrentStep = 1,
                TotalSteps = 1,
                Status = "completed",
                Approvers = new List<OrderApprover>
                {
                    new OrderApprover
                    {
                        UserId = "user_mgr_alice",
                        FullName = "Alice Manager",
                        Role = "manager",
                        Department = "Engineering",
                        StepNumber = 1,
                        ApprovalLimit = 25000,
                        Status = "approved",
                        AssignedAt = DateTime.UtcNow.AddDays(-1),
                        RespondedAt = DateTime.UtcNow.AddDays(-1).AddHours(4)
                    }
                },
                History = new List<OrderApprovalAction>
                {
                    new OrderApprovalAction
                    {
                        ActionId = Guid.NewGuid().ToString(),
                        UserId = users[8].Id,
                        UserName = $"{users[8].FirstName} {users[8].LastName}",
                        Action = "submit",
                        StepNumber = 0,
                        Comments = "Order submitted with UI/UX project details",
                        Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(-1)
                    },
                    new OrderApprovalAction
                    {
                        ActionId = Guid.NewGuid().ToString(),
                        UserId = "user_mgr_alice",
                        UserName = "Alice Manager",
                        Action = "approve",
                        StepNumber = 1,
                        Comments = "✅ APPROVED: Clear project scope with specific Adobe tools needed for UI/UX work. Good cost-benefit analysis showing potential 30% reduction in support tickets. Well-justified technical requirements.",
                        Reason = "clear_project_scope",
                        Timestamp = DateTime.UtcNow.AddDays(-1).AddHours(4)
                    }
                },
                CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-1),
                CompletedAt = DateTime.UtcNow.AddDays(-1).AddHours(4)
            },
            CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1).AddHours(4),
            SubmittedAt = DateTime.UtcNow.AddDays(-1).AddHours(-1),
            IsActive = true
        });

        await _databaseService.Orders.InsertManyAsync(scenarioOrders);
        _logger.LogInformation($"✅ Seeded {scenarioOrders.Count} scenario-based orders for AI training");
    }

    public async Task ClearAllDataAsync()
    {
        _logger.LogInformation("🧹 Clearing existing data...");

        // Clear collections in reverse dependency order
        await _databaseService.Users.DeleteManyAsync(Builders<User>.Filter.Empty);
        await _databaseService.Products.DeleteManyAsync(Builders<Product>.Filter.Empty);
        await _databaseService.Categories.DeleteManyAsync(Builders<Category>.Filter.Empty);

        _logger.LogInformation("✅ All existing data cleared");
    }

    public async Task<DatabaseSeedStatus> GetSeedStatusAsync()
    {
        var categoriesCount = await _databaseService.Categories.CountDocumentsAsync(Builders<Category>.Filter.Empty);
        var productsCount = await _databaseService.Products.CountDocumentsAsync(Builders<Product>.Filter.Empty);
        var usersCount = await _databaseService.Users.CountDocumentsAsync(Builders<User>.Filter.Empty);
        var ordersCount = await _databaseService.Orders.CountDocumentsAsync(Builders<Order>.Filter.Empty);
        var aiSessionsCount = await _databaseService.AIAgentSessions.CountDocumentsAsync(Builders<AIAgentSession>.Filter.Empty);

        return new DatabaseSeedStatus
        {
            CategoriesCount = categoriesCount,
            ProductsCount = productsCount,
            UsersCount = usersCount,
            OrdersCount = ordersCount,
            AISessionsCount = aiSessionsCount,
            IsSeeded = categoriesCount > 0 && productsCount > 0 && usersCount > 0,
            LastChecked = DateTime.UtcNow
        };
    }
}

public class DatabaseSeedStatus
{
    public long CategoriesCount { get; set; }
    public long ProductsCount { get; set; }
    public long UsersCount { get; set; }
    public long OrdersCount { get; set; }
    public long AISessionsCount { get; set; }
    public bool IsSeeded { get; set; }
    public DateTime LastChecked { get; set; }
}
