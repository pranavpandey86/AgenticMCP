using AgenticOrderingSystem.API.MCP.Interfaces;
using AgenticOrderingSystem.API.Services;
using AgenticOrderingSystem.API.Controllers;
using AgenticOrderingSystem.API.Models;
using System.Text.Json;

namespace AgenticOrderingSystem.API.MCP.Tools.OrderManagement;

/// <summary>
/// Agent tool for updating orders with new values and status changes
/// </summary>
public class UpdateOrderAgentTool : IAgentTool
{
    private readonly IOrderService _orderService;
    private readonly ILogger<UpdateOrderAgentTool> _logger;

    public UpdateOrderAgentTool(IOrderService orderService, ILogger<UpdateOrderAgentTool> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public string Name => "update_order";

    public string Description => "Update an order with new values and optionally change status from rejected to created";

    public async Task<AgentToolResult> ExecuteAsync(AgentToolContext context)
    {
        try
        {
            // Extract parameters
            if (!context.Parameters.ContainsKey("orderId"))
            {
                return new AgentToolResult
                {
                    Success = false,
                    Error = "Missing required parameter: orderId",
                    Output = "Order ID is required to update an order"
                };
            }

            var orderId = context.Parameters["orderId"].ToString()!;
            var suggestedValues = context.Parameters.GetValueOrDefault("suggested_values", new Dictionary<string, object>()) as Dictionary<string, object>;

            // Get the existing order - try by ID first, then by order number
            var existingOrder = await _orderService.GetOrderByIdAsync(orderId);
            if (existingOrder == null)
            {
                // Try by order number as fallback
                existingOrder = await _orderService.GetOrderByNumberAsync(orderId);
            }
            
            if (existingOrder == null)
            {
                return new AgentToolResult
                {
                    Success = false,
                    Error = "Order not found",
                    Output = $"Order with ID or number '{orderId}' was not found"
                };
            }

            // Verify user authorization (order owner)
            _logger.LogInformation("Authorization check: Order RequesterId = '{OrderRequesterId}', Context UserId = '{ContextUserId}'", 
                existingOrder.RequesterId, context.UserId);
            
            if (existingOrder.RequesterId != context.UserId)
            {
                return new AgentToolResult
                {
                    Success = false,
                    Error = "Unauthorized",
                    Output = $"You can only update your own orders. Order belongs to '{existingOrder.RequesterId}' but request is from '{context.UserId}'"
                };
            }

            // Verify order can be updated (must be rejected)
            if (existingOrder.Status != "rejected")
            {
                return new AgentToolResult
                {
                    Success = false,
                    Error = "Invalid order status",
                    Output = $"Order status is '{existingOrder.Status}'. Only rejected orders can be updated and resubmitted."
                };
            }

            // Apply suggested values
            var updatedFields = new List<string>();
            
            if (suggestedValues?.ContainsKey("business_justification") == true)
            {
                var newJustification = suggestedValues["business_justification"].ToString();
                if (!string.IsNullOrEmpty(newJustification))
                {
                    existingOrder.BusinessJustification = newJustification;
                    updatedFields.Add($"Business Justification: {newJustification}");
                }
            }

            if (suggestedValues?.ContainsKey("quantity") == true && int.TryParse(suggestedValues["quantity"].ToString(), out var quantity))
            {
                existingOrder.Quantity = quantity;
                updatedFields.Add($"Quantity: {quantity}");
            }

            if (suggestedValues?.ContainsKey("priority") == true)
            {
                var newPriority = suggestedValues["priority"].ToString();
                if (!string.IsNullOrEmpty(newPriority))
                {
                    existingOrder.Priority = newPriority;
                    updatedFields.Add($"Priority: {newPriority}");
                }
            }

            if (suggestedValues?.ContainsKey("required_by_date") == true && DateTime.TryParse(suggestedValues["required_by_date"].ToString(), out var requiredDate))
            {
                existingOrder.RequiredByDate = requiredDate;
                updatedFields.Add($"Required By Date: {requiredDate:yyyy-MM-dd}");
            }

            // Recalculate total amount
            existingOrder.TotalAmount = await _orderService.CalculateOrderTotalAsync(existingOrder.ProductId, existingOrder.Quantity);

            // Change status from rejected to created
            existingOrder.Status = "created";
            updatedFields.Add("Status: created (ready for resubmission)");

            // Update the order
            var updatedOrder = await _orderService.UpdateOrderAsync(existingOrder);

            _logger.LogInformation("Order {OrderId} updated successfully by agent for user {UserId}", orderId, context.UserId);

            return new AgentToolResult
            {
                Success = true,
                Output = "Order updated successfully and ready for resubmission",
                Data = new Dictionary<string, object>
                {
                    ["order_id"] = updatedOrder.Id,
                    ["updated_fields"] = updatedFields,
                    ["new_status"] = updatedOrder.Status,
                    ["total_amount"] = updatedOrder.TotalAmount,
                    ["order"] = updatedOrder
                },
                NextAction = new AgentAction
                {
                    Type = "complete",
                    Message = $"✅ Your order has been successfully updated and resubmitted for approval!\n\nUpdated fields:\n" + 
                             string.Join("\n", updatedFields.Select(f => $"• {f}"))
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId} via agent tool", context.Parameters.GetValueOrDefault("orderId", "unknown"));
            return new AgentToolResult
            {
                Success = false,
                Error = ex.Message,
                Output = "Failed to update order"
            };
        }
    }
}
