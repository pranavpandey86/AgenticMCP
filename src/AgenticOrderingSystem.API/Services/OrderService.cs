using MongoDB.Driver;
using AgenticOrderingSystem.API.Models;
using AgenticOrderingSystem.API.Services;

namespace AgenticOrderingSystem.API.Services;

/// <summary>
/// Service for managing orders and approval workflows
/// </summary>
public interface IOrderService
{
    // Order CRUD operations
    Task<Order> CreateOrderAsync(Order order);
    Task<Order?> GetOrderByIdAsync(string orderId);
    Task<Order?> GetOrderByNumberAsync(string orderNumber);
    Task<List<Order>> GetOrdersByUserAsync(string userId, int page = 1, int pageSize = 20);
    Task<List<Order>> GetOrdersByStatusAsync(string status, int page = 1, int pageSize = 20);
    Task<Order> UpdateOrderAsync(Order order);
    Task<bool> DeleteOrderAsync(string orderId);

    // Order workflow operations
    Task<Order> SubmitOrderAsync(string orderId, string userId);
    Task<Order> ApproveOrderAsync(string orderId, string approverId, string? comments = null);
    Task<Order> RejectOrderAsync(string orderId, string approverId, string reason, string? comments = null);
    Task<Order> RequestMoreInfoAsync(string orderId, string approverId, string requestDetails);
    Task<Order> CancelOrderAsync(string orderId, string userId, string reason);

    // Approval workflow management
    Task<OrderApprovalWorkflow> BuildApprovalWorkflowAsync(Order order);
    Task<List<OrderApprover>> GetPendingApproversAsync(string orderId);
    Task<bool> CanUserApproveAsync(string orderId, string userId);
    Task<List<Order>> GetOrdersForApprovalAsync(string userId, int page = 1, int pageSize = 20);

    // Order search and filtering
    Task<List<Order>> SearchOrdersAsync(OrderSearchCriteria criteria);
    Task<OrderStatistics> GetOrderStatisticsAsync(string? userId = null, DateTime? startDate = null, DateTime? endDate = null);

    // Utility methods
    Task<string> GenerateOrderNumberAsync();
    Task<decimal> CalculateOrderTotalAsync(string productId, int quantity);
}

/// <summary>
/// Order search criteria
/// </summary>
public class OrderSearchCriteria
{
    public string? RequesterId { get; set; }
    public string? ProductCategory { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Order statistics
/// </summary>
public class OrderStatistics
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ApprovedOrders { get; set; }
    public int RejectedOrders { get; set; }
    public decimal TotalValue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public Dictionary<string, int> OrdersByStatus { get; set; } = new();
    public Dictionary<string, int> OrdersByCategory { get; set; } = new();
    public Dictionary<string, decimal> ValueByCategory { get; set; } = new();
}

public class OrderService : IOrderService
{
    private readonly IDatabaseService _databaseService;
    private readonly IUserService _userService;
    private readonly ILogger<OrderService> _logger;
    private readonly IConfiguration _configuration;

    public OrderService(
        IDatabaseService databaseService,
        IUserService userService,
        ILogger<OrderService> logger,
        IConfiguration configuration)
    {
        _databaseService = databaseService;
        _userService = userService;
        _logger = logger;
        _configuration = configuration;
    }

    #region Order CRUD Operations

    public async Task<Order> CreateOrderAsync(Order order)
    {
        try
        {
            order.Id = Guid.NewGuid().ToString();
            order.OrderNumber = await GenerateOrderNumberAsync();
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            // Build approval workflow
            order.ApprovalWorkflow = await BuildApprovalWorkflowAsync(order);

            await _databaseService.Orders.InsertOneAsync(order);
            
            _logger.LogInformation("Order created successfully: {OrderId} - {OrderNumber}", order.Id, order.OrderNumber);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order");
            throw;
        }
    }

    public async Task<Order?> GetOrderByIdAsync(string orderId)
    {
        try
        {
            var filter = Builders<Order>.Filter.Eq(o => o.Id, orderId);
            return await _databaseService.Orders.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order by ID: {OrderId}", orderId);
            throw;
        }
    }

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
    {
        try
        {
            var filter = Builders<Order>.Filter.Eq(o => o.OrderNumber, orderNumber);
            return await _databaseService.Orders.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order by number: {OrderNumber}", orderNumber);
            throw;
        }
    }

    public async Task<List<Order>> GetOrdersByUserAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var filter = Builders<Order>.Filter.Eq(o => o.RequesterId, userId);
            var skip = (page - 1) * pageSize;
            
            return await _databaseService.Orders
                .Find(filter)
                .SortByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<List<Order>> GetOrdersByStatusAsync(string status, int page = 1, int pageSize = 20)
    {
        try
        {
            var filter = Builders<Order>.Filter.Eq(o => o.Status, status);
            var skip = (page - 1) * pageSize;
            
            return await _databaseService.Orders
                .Find(filter)
                .SortByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders by status: {Status}", status);
            throw;
        }
    }

    public async Task<Order> UpdateOrderAsync(Order order)
    {
        try
        {
            order.UpdatedAt = DateTime.UtcNow;
            
            var filter = Builders<Order>.Filter.Eq(o => o.Id, order.Id);
            await _databaseService.Orders.ReplaceOneAsync(filter, order);
            
            _logger.LogInformation("Order updated successfully: {OrderId}", order.Id);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order: {OrderId}", order.Id);
            throw;
        }
    }

    public async Task<bool> DeleteOrderAsync(string orderId)
    {
        try
        {
            var filter = Builders<Order>.Filter.Eq(o => o.Id, orderId);
            var result = await _databaseService.Orders.DeleteOneAsync(filter);
            
            var deleted = result.DeletedCount > 0;
            if (deleted)
            {
                _logger.LogInformation("Order deleted successfully: {OrderId}", orderId);
            }
            
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete order: {OrderId}", orderId);
            throw;
        }
    }

    #endregion

    #region Order Workflow Operations

    public async Task<Order> SubmitOrderAsync(string orderId, string userId)
    {
        try
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException($"Order not found: {orderId}");

            if (order.RequesterId != userId)
                throw new UnauthorizedAccessException("User can only submit their own orders");

            if (order.Status != "draft")
                throw new InvalidOperationException($"Order cannot be submitted in current status: {order.Status}");

            // Update order status
            order.Status = "submitted";
            order.SubmittedAt = DateTime.UtcNow;
            order.ApprovalWorkflow.Status = "in_progress";

            // Add submission action to history
            var submitAction = new OrderApprovalAction
            {
                UserId = userId,
                UserName = order.RequesterInfo.FullName,
                Action = "submit",
                StepNumber = 0,
                Comments = "Order submitted for approval"
            };
            order.ApprovalWorkflow.History.Add(submitAction);

            return await UpdateOrderAsync(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit order: {OrderId}", orderId);
            throw;
        }
    }

    public async Task<Order> ApproveOrderAsync(string orderId, string approverId, string? comments = null)
    {
        try
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException($"Order not found: {orderId}");

            if (!await CanUserApproveAsync(orderId, approverId))
                throw new UnauthorizedAccessException("User is not authorized to approve this order");

            // Find the current approver
            var currentApprover = order.ApprovalWorkflow.Approvers
                .FirstOrDefault(a => a.UserId == approverId && a.Status == "pending");

            if (currentApprover == null)
                throw new InvalidOperationException("No pending approval found for this user");

            // Update approver status
            currentApprover.Status = "approved";
            currentApprover.RespondedAt = DateTime.UtcNow;

            // Add approval action to history
            var approvalAction = new OrderApprovalAction
            {
                UserId = approverId,
                UserName = currentApprover.FullName,
                Action = "approve",
                StepNumber = currentApprover.StepNumber,
                Comments = comments ?? string.Empty
            };
            order.ApprovalWorkflow.History.Add(approvalAction);

            // Check if all required approvals are complete
            var pendingRequiredApprovers = order.ApprovalWorkflow.Approvers
                .Where(a => a.IsRequired && a.Status == "pending")
                .ToList();

            if (!pendingRequiredApprovers.Any())
            {
                // All approvals complete
                order.Status = "approved";
                order.ApprovalWorkflow.Status = "approved";
                order.ApprovalWorkflow.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                // Move to next step
                order.ApprovalWorkflow.CurrentStep++;
            }

            return await UpdateOrderAsync(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve order: {OrderId}", orderId);
            throw;
        }
    }

    public async Task<Order> RejectOrderAsync(string orderId, string approverId, string reason, string? comments = null)
    {
        try
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException($"Order not found: {orderId}");

            if (!await CanUserApproveAsync(orderId, approverId))
                throw new UnauthorizedAccessException("User is not authorized to reject this order");

            // Update order status
            order.Status = "rejected";
            order.ApprovalWorkflow.Status = "rejected";
            order.ApprovalWorkflow.CompletedAt = DateTime.UtcNow;

            // Find and update the current approver
            var currentApprover = order.ApprovalWorkflow.Approvers
                .FirstOrDefault(a => a.UserId == approverId && a.Status == "pending");

            if (currentApprover != null)
            {
                currentApprover.Status = "rejected";
                currentApprover.RespondedAt = DateTime.UtcNow;
            }

            // Add rejection action to history
            var rejectionAction = new OrderApprovalAction
            {
                UserId = approverId,
                UserName = currentApprover?.FullName ?? "Unknown",
                Action = "reject",
                StepNumber = currentApprover?.StepNumber ?? 0,
                Reason = reason,
                Comments = comments ?? string.Empty
            };
            order.ApprovalWorkflow.History.Add(rejectionAction);

            return await UpdateOrderAsync(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject order: {OrderId}", orderId);
            throw;
        }
    }

    public async Task<Order> RequestMoreInfoAsync(string orderId, string approverId, string requestDetails)
    {
        try
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException($"Order not found: {orderId}");

            if (!await CanUserApproveAsync(orderId, approverId))
                throw new UnauthorizedAccessException("User is not authorized to request information for this order");

            // Add request action to history
            var requestAction = new OrderApprovalAction
            {
                UserId = approverId,
                Action = "request_info",
                Comments = requestDetails
            };
            order.ApprovalWorkflow.History.Add(requestAction);

            // You might want to update status to "info_requested" or similar
            // and potentially notify the requester

            return await UpdateOrderAsync(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request more info for order: {OrderId}", orderId);
            throw;
        }
    }

    public async Task<Order> CancelOrderAsync(string orderId, string userId, string reason)
    {
        try
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException($"Order not found: {orderId}");

            if (order.RequesterId != userId)
                throw new UnauthorizedAccessException("User can only cancel their own orders");

            order.Status = "cancelled";
            order.ApprovalWorkflow.Status = "cancelled";
            order.ApprovalWorkflow.CompletedAt = DateTime.UtcNow;

            // Add cancellation action to history
            var cancelAction = new OrderApprovalAction
            {
                UserId = userId,
                UserName = order.RequesterInfo.FullName,
                Action = "cancel",
                Reason = reason
            };
            order.ApprovalWorkflow.History.Add(cancelAction);

            return await UpdateOrderAsync(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel order: {OrderId}", orderId);
            throw;
        }
    }

    #endregion

    #region Approval Workflow Management

    public async Task<OrderApprovalWorkflow> BuildApprovalWorkflowAsync(Order order)
    {
        try
        {
            var workflow = new OrderApprovalWorkflow
            {
                IsRequired = true,
                CurrentStep = 1
            };

            // Get the requester
            var requester = await _userService.GetUserByIdAsync(order.RequesterId);
            if (requester == null)
                throw new ArgumentException($"Requester not found: {order.RequesterId}");

            var approvers = new List<OrderApprover>();
            var stepNumber = 1;

            // Get product for approval rules
            var product = await _databaseService.Products.Find(p => p.Id == order.ProductId).FirstOrDefaultAsync();
            if (product?.ApprovalModel == null)
                throw new ArgumentException($"Product approval model not found: {order.ProductId}");

            // Build approval chain based on product rules and order amount
            var approvalLevels = new List<ApprovalLevel>();
            
            // Add approval levels based on product model
            if (product.ApprovalModel.Level1.Required)
                approvalLevels.Add(product.ApprovalModel.Level1);
            
            if (product.ApprovalModel.Level2.Required)
                approvalLevels.Add(product.ApprovalModel.Level2);

            foreach (var level in approvalLevels)
            {
                // Check if this level applies to the order amount
                if (order.TotalAmount >= level.TriggerConditions.MinAmount && 
                    (level.TriggerConditions.MaxAmount == 0 || order.TotalAmount <= level.TriggerConditions.MaxAmount))
                {
                    // Find approver by role or specific user
                    var approver = await FindApproverByLevelAsync(level, requester.Department, order.TotalAmount);
                    if (approver != null)
                    {
                        approvers.Add(new OrderApprover
                        {
                            UserId = approver.Id,
                            FullName = $"{approver.FirstName} {approver.LastName}",
                            Role = approver.Role,
                            Department = approver.Department,
                            StepNumber = stepNumber++,
                            ApprovalLimit = approver.ApprovalAuthority.MaxApprovalAmount,
                            IsRequired = level.Required,
                            Deadline = DateTime.UtcNow.AddHours(level.TimeoutHours)
                        });
                    }
                }
            }

            workflow.Approvers = approvers;
            workflow.TotalSteps = approvers.Count;

            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build approval workflow for order");
            throw;
        }
    }

    private async Task<User?> FindApproverByLevelAsync(ApprovalLevel level, string requesterDepartment, decimal orderAmount)
    {
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Eq(u => u.IsActive, true),
            Builders<User>.Filter.Gte(u => u.ApprovalAuthority.MaxApprovalAmount, orderAmount),
            Builders<User>.Filter.Eq(u => u.ApprovalAuthority.CanApprove, true)
        );

        // Add role filter if specified
        if (!string.IsNullOrEmpty(level.ApproverType))
        {
            filter = Builders<User>.Filter.And(filter, 
                Builders<User>.Filter.Eq(u => u.Role, level.ApproverType));
        }

        // For manager approval, find someone in the same department with approval authority
        if (level.ApproverType == "manager")
        {
            filter = Builders<User>.Filter.And(filter,
                Builders<User>.Filter.Eq(u => u.Department, requesterDepartment));
        }

        return await _databaseService.Users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<OrderApprover>> GetPendingApproversAsync(string orderId)
    {
        try
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null)
                return new List<OrderApprover>();

            return order.ApprovalWorkflow.Approvers
                .Where(a => a.Status == "pending")
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending approvers for order: {OrderId}", orderId);
            throw;
        }
    }

    public async Task<bool> CanUserApproveAsync(string orderId, string userId)
    {
        try
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null)
                return false;

            return order.ApprovalWorkflow.Approvers
                .Any(a => a.UserId == userId && a.Status == "pending");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if user can approve order: {OrderId}, {UserId}", orderId, userId);
            return false;
        }
    }

    public async Task<List<Order>> GetOrdersForApprovalAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            var filter = Builders<Order>.Filter.And(
                Builders<Order>.Filter.ElemMatch(o => o.ApprovalWorkflow.Approvers,
                    Builders<OrderApprover>.Filter.And(
                        Builders<OrderApprover>.Filter.Eq(a => a.UserId, userId),
                        Builders<OrderApprover>.Filter.Eq(a => a.Status, "pending")
                    )
                ),
                Builders<Order>.Filter.In(o => o.Status, new[] { "submitted", "under_review" })
            );

            var skip = (page - 1) * pageSize;
            
            return await _databaseService.Orders
                .Find(filter)
                .SortByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders for approval: {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Search and Statistics

    public async Task<List<Order>> SearchOrdersAsync(OrderSearchCriteria criteria)
    {
        try
        {
            var filterBuilder = Builders<Order>.Filter;
            var filters = new List<FilterDefinition<Order>>();

            // Add filters based on criteria
            if (!string.IsNullOrEmpty(criteria.RequesterId))
                filters.Add(filterBuilder.Eq(o => o.RequesterId, criteria.RequesterId));

            if (!string.IsNullOrEmpty(criteria.Status))
                filters.Add(filterBuilder.Eq(o => o.Status, criteria.Status));

            if (!string.IsNullOrEmpty(criteria.Priority))
                filters.Add(filterBuilder.Eq(o => o.Priority, criteria.Priority));

            if (!string.IsNullOrEmpty(criteria.ProductCategory))
                filters.Add(filterBuilder.Eq(o => o.ProductInfo.Category, criteria.ProductCategory));

            if (criteria.MinAmount.HasValue)
                filters.Add(filterBuilder.Gte(o => o.TotalAmount, criteria.MinAmount.Value));

            if (criteria.MaxAmount.HasValue)
                filters.Add(filterBuilder.Lte(o => o.TotalAmount, criteria.MaxAmount.Value));

            if (criteria.StartDate.HasValue)
                filters.Add(filterBuilder.Gte(o => o.CreatedAt, criteria.StartDate.Value));

            if (criteria.EndDate.HasValue)
                filters.Add(filterBuilder.Lte(o => o.CreatedAt, criteria.EndDate.Value));

            if (!string.IsNullOrEmpty(criteria.SearchText))
            {
                var textFilter = filterBuilder.Or(
                    filterBuilder.Regex(o => o.OrderNumber, new MongoDB.Bson.BsonRegularExpression(criteria.SearchText, "i")),
                    filterBuilder.Regex(o => o.ProductInfo.Name, new MongoDB.Bson.BsonRegularExpression(criteria.SearchText, "i")),
                    filterBuilder.Regex(o => o.BusinessJustification, new MongoDB.Bson.BsonRegularExpression(criteria.SearchText, "i"))
                );
                filters.Add(textFilter);
            }

            var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;
            var skip = (criteria.Page - 1) * criteria.PageSize;

            return await _databaseService.Orders
                .Find(finalFilter)
                .SortByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Limit(criteria.PageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search orders");
            throw;
        }
    }

    public async Task<OrderStatistics> GetOrderStatisticsAsync(string? userId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var filterBuilder = Builders<Order>.Filter;
            var filters = new List<FilterDefinition<Order>>();

            if (!string.IsNullOrEmpty(userId))
                filters.Add(filterBuilder.Eq(o => o.RequesterId, userId));

            if (startDate.HasValue)
                filters.Add(filterBuilder.Gte(o => o.CreatedAt, startDate.Value));

            if (endDate.HasValue)
                filters.Add(filterBuilder.Lte(o => o.CreatedAt, endDate.Value));

            var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;
            var orders = await _databaseService.Orders.Find(finalFilter).ToListAsync();

            var stats = new OrderStatistics
            {
                TotalOrders = orders.Count,
                PendingOrders = orders.Count(o => o.Status == "submitted" || o.Status == "under_review"),
                ApprovedOrders = orders.Count(o => o.Status == "approved"),
                RejectedOrders = orders.Count(o => o.Status == "rejected"),
                TotalValue = orders.Sum(o => o.TotalAmount),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0
            };

            stats.OrdersByStatus = orders
                .GroupBy(o => o.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            stats.OrdersByCategory = orders
                .GroupBy(o => o.ProductInfo.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            stats.ValueByCategory = orders
                .GroupBy(o => o.ProductInfo.Category)
                .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount));

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order statistics");
            throw;
        }
    }

    #endregion

    #region Utility Methods

    public async Task<string> GenerateOrderNumberAsync()
    {
        try
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month.ToString("D2");
            
            // Get the latest order number for this month
            var prefix = $"ORD-{year}-{month}";
            var filter = Builders<Order>.Filter.Regex(o => o.OrderNumber, new MongoDB.Bson.BsonRegularExpression($"^{prefix}"));
            var latestOrder = await _databaseService.Orders
                .Find(filter)
                .SortByDescending(o => o.OrderNumber)
                .FirstOrDefaultAsync();

            var sequence = 1;
            if (latestOrder != null)
            {
                // Extract sequence number from the latest order
                var parts = latestOrder.OrderNumber.Split('-');
                if (parts.Length >= 4 && int.TryParse(parts[3], out var lastSequence))
                {
                    sequence = lastSequence + 1;
                }
            }

            return $"{prefix}-{sequence:D4}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate order number");
            throw;
        }
    }

    public async Task<decimal> CalculateOrderTotalAsync(string productId, int quantity)
    {
        try
        {
            var product = await _databaseService.Products.Find(p => p.Id == productId).FirstOrDefaultAsync();
            if (product == null)
                throw new ArgumentException($"Product not found: {productId}");

            return product.Price * quantity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate order total for product: {ProductId}", productId);
            throw;
        }
    }

    #endregion
}
