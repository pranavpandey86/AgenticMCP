using Microsoft.AspNetCore.Mvc;
using AgenticOrderingSystem.API.Models;
using AgenticOrderingSystem.API.Services;

namespace AgenticOrderingSystem.API.Controllers;

/// <summary>
/// API controller for managing orders and approval workflows
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IUserService _userService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        IOrderService orderService,
        IUserService userService,
        ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _userService = userService;
        _logger = logger;
    }

    #region Order CRUD Operations

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrEmpty(request.RequesterId) || string.IsNullOrEmpty(request.ProductId))
            {
                return BadRequest("RequesterId and ProductId are required");
            }

            // Build order from request
            var order = await BuildOrderFromRequestAsync(request);
            
            // Create the order
            var createdOrder = await _orderService.CreateOrderAsync(order);
            
            _logger.LogInformation("Order created successfully: {OrderId}", createdOrder.Id);
            return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, createdOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order");
            return StatusCode(500, new { error = "Failed to create order", message = ex.Message });
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrderById(string id)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound($"Order not found: {id}");
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order: {OrderId}", id);
            return StatusCode(500, new { error = "Failed to get order", message = ex.Message });
        }
    }

    /// <summary>
    /// Get order by order number
    /// </summary>
    [HttpGet("number/{orderNumber}")]
    public async Task<ActionResult<Order>> GetOrderByNumber(string orderNumber)
    {
        try
        {
            var order = await _orderService.GetOrderByNumberAsync(orderNumber);
            if (order == null)
            {
                return NotFound($"Order not found: {orderNumber}");
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order by number: {OrderNumber}", orderNumber);
            return StatusCode(500, new { error = "Failed to get order", message = ex.Message });
        }
    }

    /// <summary>
    /// Get orders for a specific user
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<Order>>> GetOrdersByUser(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var orders = await _orderService.GetOrdersByUserAsync(userId, page, pageSize);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders for user: {UserId}", userId);
            return StatusCode(500, new { error = "Failed to get orders", message = ex.Message });
        }
    }

    /// <summary>
    /// Get orders by status
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<List<Order>>> GetOrdersByStatus(string status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var orders = await _orderService.GetOrdersByStatusAsync(status, page, pageSize);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders by status: {Status}", status);
            return StatusCode(500, new { error = "Failed to get orders", message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing order
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Order>> UpdateOrder(string id, [FromBody] UpdateOrderRequest request)
    {
        try
        {
            var existingOrder = await _orderService.GetOrderByIdAsync(id);
            if (existingOrder == null)
            {
                return NotFound($"Order not found: {id}");
            }

            // Update order fields
            if (!string.IsNullOrEmpty(request.BusinessJustification))
                existingOrder.BusinessJustification = request.BusinessJustification;

            if (request.Quantity.HasValue)
                existingOrder.Quantity = request.Quantity.Value;

            if (request.RequiredByDate.HasValue)
                existingOrder.RequiredByDate = request.RequiredByDate;

            if (!string.IsNullOrEmpty(request.Priority))
                existingOrder.Priority = request.Priority;

            // Update custom responses if provided
            if (request.CustomResponses?.Any() == true)
            {
                existingOrder.CustomResponses = request.CustomResponses;
            }

            // Recalculate total
            existingOrder.TotalAmount = await _orderService.CalculateOrderTotalAsync(existingOrder.ProductId, existingOrder.Quantity);

            var updatedOrder = await _orderService.UpdateOrderAsync(existingOrder);
            return Ok(updatedOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order: {OrderId}", id);
            return StatusCode(500, new { error = "Failed to update order", message = ex.Message });
        }
    }

    /// <summary>
    /// Delete an order
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteOrder(string id)
    {
        try
        {
            var deleted = await _orderService.DeleteOrderAsync(id);
            if (!deleted)
            {
                return NotFound($"Order not found: {id}");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete order: {OrderId}", id);
            return StatusCode(500, new { error = "Failed to delete order", message = ex.Message });
        }
    }

    #endregion

    #region Order Workflow Operations

    /// <summary>
    /// Submit order for approval
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult<Order>> SubmitOrder(string id, [FromBody] SubmitOrderRequest request)
    {
        try
        {
            var order = await _orderService.SubmitOrderAsync(id, request.UserId);
            return Ok(order);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit order: {OrderId}", id);
            return StatusCode(500, new { error = "Failed to submit order", message = ex.Message });
        }
    }

    /// <summary>
    /// Approve an order
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<Order>> ApproveOrder(string id, [FromBody] ApprovalActionRequest request)
    {
        try
        {
            var order = await _orderService.ApproveOrderAsync(id, request.UserId, request.Comments);
            return Ok(order);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve order: {OrderId}", id);
            return StatusCode(500, new { error = "Failed to approve order", message = ex.Message });
        }
    }

    /// <summary>
    /// Reject an order
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult<Order>> RejectOrder(string id, [FromBody] RejectionRequest request)
    {
        try
        {
            var order = await _orderService.RejectOrderAsync(id, request.UserId, request.Reason, request.Comments);
            return Ok(order);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject order: {OrderId}", id);
            return StatusCode(500, new { error = "Failed to reject order", message = ex.Message });
        }
    }

    /// <summary>
    /// Request more information for an order
    /// </summary>
    [HttpPost("{id}/request-info")]
    public async Task<ActionResult<Order>> RequestMoreInfo(string id, [FromBody] InfoRequestRequest request)
    {
        try
        {
            var order = await _orderService.RequestMoreInfoAsync(id, request.UserId, request.RequestDetails);
            return Ok(order);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request info for order: {OrderId}", id);
            return StatusCode(500, new { error = "Failed to request information", message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<Order>> CancelOrder(string id, [FromBody] CancelOrderRequest request)
    {
        try
        {
            var order = await _orderService.CancelOrderAsync(id, request.UserId, request.Reason);
            return Ok(order);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel order: {OrderId}", id);
            return StatusCode(500, new { error = "Failed to cancel order", message = ex.Message });
        }
    }

    /// <summary>
    /// Get orders pending approval for a specific user
    /// </summary>
    [HttpGet("pending-approval/{userId}")]
    public async Task<ActionResult<List<Order>>> GetOrdersForApproval(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var orders = await _orderService.GetOrdersForApprovalAsync(userId, page, pageSize);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders for approval: {UserId}", userId);
            return StatusCode(500, new { error = "Failed to get orders for approval", message = ex.Message });
        }
    }

    /// <summary>
    /// Check if user can approve an order
    /// </summary>
    [HttpGet("{id}/can-approve/{userId}")]
    public async Task<ActionResult<bool>> CanUserApprove(string id, string userId)
    {
        try
        {
            var canApprove = await _orderService.CanUserApproveAsync(id, userId);
            return Ok(new { canApprove });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check approval permission: {OrderId}, {UserId}", id, userId);
            return StatusCode(500, new { error = "Failed to check approval permission", message = ex.Message });
        }
    }

    #endregion

    #region Search and Analytics

    /// <summary>
    /// Search orders with various criteria
    /// </summary>
    [HttpPost("search")]
    public async Task<ActionResult<List<Order>>> SearchOrders([FromBody] OrderSearchCriteria criteria)
    {
        try
        {
            var orders = await _orderService.SearchOrdersAsync(criteria);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search orders");
            return StatusCode(500, new { error = "Failed to search orders", message = ex.Message });
        }
    }

    /// <summary>
    /// Get order statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<OrderStatistics>> GetOrderStatistics(
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var stats = await _orderService.GetOrderStatisticsAsync(userId, startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get order statistics");
            return StatusCode(500, new { error = "Failed to get order statistics", message = ex.Message });
        }
    }

    #endregion

    #region Helper Methods

    private async Task<Order> BuildOrderFromRequestAsync(CreateOrderRequest request)
    {
        // Get requester information
        var requester = await _userService.GetUserByIdAsync(request.RequesterId);
        if (requester == null)
            throw new ArgumentException($"Requester not found: {request.RequesterId}");

        // Get product information
        var product = await _orderService.CalculateOrderTotalAsync(request.ProductId, request.Quantity);

        var order = new Order
        {
            RequesterId = request.RequesterId,
            RequesterInfo = new OrderRequesterInfo
            {
                UserId = requester.Id,
                FullName = $"{requester.FirstName} {requester.LastName}",
                Email = requester.Email,
                Department = requester.Department,
                Role = requester.Role,
                ManagerId = requester.ManagerId,
                ManagerName = !string.IsNullOrEmpty(requester.ManagerId) 
                    ? (await _userService.GetUserByIdAsync(requester.ManagerId))?.FirstName + " " + 
                      (await _userService.GetUserByIdAsync(requester.ManagerId))?.LastName 
                    : null
            },
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            TotalAmount = product,
            BusinessJustification = request.BusinessJustification ?? string.Empty,
            Priority = request.Priority ?? "medium",
            RequiredByDate = request.RequiredByDate,
            CustomResponses = request.CustomResponses ?? new List<OrderCustomResponse>(),
            Status = "draft"
        };

        return order;
    }

    #endregion
}

#region Request/Response Models

public class CreateOrderRequest
{
    public string RequesterId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string? BusinessJustification { get; set; }
    public string? Priority { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public List<OrderCustomResponse>? CustomResponses { get; set; }
}

public class UpdateOrderRequest
{
    public string? BusinessJustification { get; set; }
    public int? Quantity { get; set; }
    public DateTime? RequiredByDate { get; set; }
    public string? Priority { get; set; }
    public List<OrderCustomResponse>? CustomResponses { get; set; }
}

public class SubmitOrderRequest
{
    public string UserId { get; set; } = string.Empty;
}

public class ApprovalActionRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

public class RejectionRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

public class InfoRequestRequest
{
    public string UserId { get; set; } = string.Empty;
    public string RequestDetails { get; set; } = string.Empty;
}

public class CancelOrderRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

#endregion
