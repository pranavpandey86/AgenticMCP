using Microsoft.AspNetCore.Mvc;
using AgenticOrderingSystem.API.Services;
using AgenticOrderingSystem.API.Models;
using MongoDB.Driver;

namespace AgenticOrderingSystem.API.Controllers;

/// <summary>
/// Controller for database seeding and development utilities
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DevController : ControllerBase
{
    private readonly IDataSeedingService _dataSeedingService;
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<DevController> _logger;

    public DevController(
        IDataSeedingService dataSeedingService,
        IDatabaseService databaseService,
        ILogger<DevController> logger)
    {
        _dataSeedingService = dataSeedingService;
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get database connection health and seed status
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealthStatus()
    {
        try
        {
            var isConnected = await _databaseService.TestConnectionAsync();
            var seedStatus = await _dataSeedingService.GetSeedStatusAsync();

            var healthInfo = new
            {
                DatabaseConnected = isConnected,
                Timestamp = DateTime.UtcNow,
                SeedStatus = seedStatus,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            };

            return Ok(healthInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { Error = "Health check failed", Message = ex.Message });
        }
    }

    /// <summary>
    /// Seed the database with mock data
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> SeedDatabase()
    {
        try
        {
            _logger.LogInformation("Seeding database via API request");
            await _dataSeedingService.SeedAllDataAsync();

            var seedStatus = await _dataSeedingService.GetSeedStatusAsync();
            return Ok(new
            {
                Message = "Database seeded successfully",
                SeedStatus = seedStatus,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed database");
            return StatusCode(500, new { Error = "Failed to seed database", Message = ex.Message });
        }
    }

    /// <summary>
    /// Clear all data from the database
    /// </summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearDatabase()
    {
        try
        {
            _logger.LogInformation("Clearing database via API request");
            await _dataSeedingService.ClearAllDataAsync();

            return Ok(new
            {
                Message = "Database cleared successfully",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear database");
            return StatusCode(500, new { Error = "Failed to clear database", Message = ex.Message });
        }
    }

    /// <summary>
    /// Get sample data counts for verification
    /// </summary>
    [HttpGet("data-summary")]
    public async Task<IActionResult> GetDataSummary()
    {
        try
        {
            var categories = await _databaseService.Categories.Find(_ => true).ToListAsync();
            var products = await _databaseService.Products.Find(_ => true).ToListAsync();
            var users = await _databaseService.Users.Find(_ => true).ToListAsync();

            var summary = new
            {
                Categories = new
                {
                    Count = categories.Count,
                    Items = categories.Select(c => new { c.Id, c.Name, c.IsActive }).ToList()
                },
                Products = new
                {
                    Count = products.Count,
                    Items = products.Select(p => new { p.Id, p.Name, p.Category, p.IsActive }).ToList()
                },
                Users = new
                {
                    Count = users.Count,
                    Items = users.Select(u => new { u.Id, u.FullName, u.Role, u.Department, u.IsActive }).ToList()
                },
                UserHierarchy = BuildUserHierarchy(users)
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data summary");
            return StatusCode(500, new { Error = "Failed to get data summary", Message = ex.Message });
        }
    }

    private object BuildUserHierarchy(List<AgenticOrderingSystem.API.Models.User> users)
    {
        var hierarchy = new Dictionary<string, object>();

        // Find top-level users (MD)
        var topLevel = users.Where(u => u.ManagerId == null).ToList();

        foreach (var user in topLevel)
        {
            hierarchy[user.Role] = BuildUserBranch(user, users);
        }

        return hierarchy;
    }

    private object BuildUserBranch(AgenticOrderingSystem.API.Models.User user, List<AgenticOrderingSystem.API.Models.User> allUsers)
    {
        var directReports = allUsers.Where(u => u.ManagerId == user.Id).ToList();

        return new
        {
            Name = user.FullName,
            Role = user.Role,
            Department = user.Department,
            CanApprove = user.ApprovalAuthority.CanApprove,
            MaxApproval = user.ApprovalAuthority.MaxApprovalAmount,
            DirectReports = directReports.Select(dr => BuildUserBranch(dr, allUsers)).ToList()
        };
    }

    /// <summary>
    /// Create a sample order for testing
    /// </summary>
    [HttpPost("create-sample-order")]
    public async Task<IActionResult> CreateSampleOrder()
    {
        try
        {
            // Get first active user and product for testing
            var user = await _databaseService.Users.Find(u => u.IsActive).FirstAsync();
            var product = await _databaseService.Products.Find(p => p.IsActive).FirstAsync();

            if (user == null || product == null)
            {
                return BadRequest("No active users or products found. Please seed the database first.");
            }

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                OrderNumber = $"TEST-{DateTime.UtcNow:yyyyMMdd}-001",
                RequesterId = user.Id,
                RequesterInfo = new OrderRequesterInfo
                {
                    UserId = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email,
                    Department = user.Department,
                    Role = user.Role,
                    ManagerId = user.ManagerId
                },
                ProductId = product.Id,
                ProductInfo = new OrderProductInfo
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Category = product.Category,
                    UnitPrice = product.Price,
                    Currency = "USD",
                    Vendor = product.Metadata?.Vendor ?? "Unknown"
                },
                Quantity = 1,
                TotalAmount = product.Price,
                Currency = "USD",
                Status = "draft",
                Priority = "medium",
                BusinessJustification = "Sample order for testing Phase A implementation",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _databaseService.Orders.InsertOneAsync(order);

            _logger.LogInformation("Sample order created: {OrderId} - {OrderNumber}", order.Id, order.OrderNumber);

            return Ok(new
            {
                Message = "Sample order created successfully",
                Order = new
                {
                    order.Id,
                    order.OrderNumber,
                    RequesterName = order.RequesterInfo.FullName,
                    ProductName = order.ProductInfo.Name,
                    order.TotalAmount,
                    order.Status,
                    order.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create sample order");
            return StatusCode(500, new { Error = "Failed to create sample order", Message = ex.Message });
        }
    }

    /// <summary>
    /// Seed some rejected orders for testing approval workflows
    /// </summary>
    [HttpPost("seed-rejected-orders")]
    public async Task<IActionResult> SeedRejectedOrders()
    {
        try
        {
            _logger.LogInformation("Seeding rejected orders via API request");
            await _dataSeedingService.SeedRejectedOrdersAsync();

            var seedStatus = await _dataSeedingService.GetSeedStatusAsync();
            return Ok(new
            {
                Message = "Rejected orders seeded successfully",
                OrdersCount = seedStatus.OrdersCount,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed rejected orders");
            return StatusCode(500, new { Error = "Failed to seed rejected orders", Message = ex.Message });
        }
    }

    /// <summary>
    /// Seed scenario-based orders for AI training (same products, different outcomes)
    /// </summary>
    [HttpPost("seed-scenario-orders")]
    public async Task<IActionResult> SeedScenarioOrders()
    {
        try
        {
            _logger.LogInformation("Seeding scenario-based orders via API request");
            await _dataSeedingService.SeedScenarioBasedOrdersAsync();

            var seedStatus = await _dataSeedingService.GetSeedStatusAsync();
            return Ok(new
            {
                Message = "Scenario-based orders seeded successfully for AI training",
                OrdersCount = seedStatus.OrdersCount,
                Description = "Added orders showing same products with different justifications leading to different approval outcomes",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed scenario-based orders");
            return StatusCode(500, new { Error = "Failed to seed scenario-based orders", Message = ex.Message });
        }
    }
}
