using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace AgenticOrderingSystem.API.Tests.MCP.Tools
{
    // Mock classes for MCP Tool testing
    public class Order
    {
        public string Id { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AgentToolContext
    {
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    public class AgentToolResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public object? Data { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public interface IOrderService
    {
        Task<Order?> GetOrderByIdAsync(string orderId);
        Task<Order?> GetOrderByNumberAsync(string orderNumber);
        Task<Order?> UpdateOrderAsync(string orderId, Order order);
        Task<bool> DeleteOrderAsync(string orderId);
    }

    public class UpdateOrderAgentTool
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<UpdateOrderAgentTool> _logger;

        public UpdateOrderAgentTool(IOrderService orderService, ILogger<UpdateOrderAgentTool> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        public async Task<AgentToolResult> ExecuteAsync(AgentToolContext context)
        {
            try
            {
                if (!context.Parameters.TryGetValue("orderId", out var orderIdObj) || orderIdObj == null)
                {
                    return new AgentToolResult
                    {
                        Success = false,
                        Error = "Order ID parameter is required"
                    };
                }

                var orderId = orderIdObj.ToString()!;
                _logger.LogInformation($"Updating order {orderId}");

                // Try to find order by ID first, then by order number
                var order = await _orderService.GetOrderByIdAsync(orderId) 
                           ?? await _orderService.GetOrderByNumberAsync(orderId);

                if (order == null)
                {
                    return new AgentToolResult
                    {
                        Success = false,
                        Error = "Order not found"
                    };
                }

                // Update order properties from parameters
                if (context.Parameters.TryGetValue("status", out var statusObj))
                {
                    order.Status = statusObj.ToString()!;
                }

                if (context.Parameters.TryGetValue("totalAmount", out var amountObj) && 
                    decimal.TryParse(amountObj.ToString(), out var amount))
                {
                    order.TotalAmount = amount;
                }

                var updatedOrder = await _orderService.UpdateOrderAsync(order.Id, order);
                
                if (updatedOrder != null)
                {
                    return new AgentToolResult
                    {
                        Success = true,
                        Data = updatedOrder,
                        Message = "Order updated successfully"
                    };
                }

                return new AgentToolResult
                {
                    Success = false,
                    Error = "Failed to update order"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order");
                return new AgentToolResult
                {
                    Success = false,
                    Error = $"Error updating order: {ex.Message}"
                };
            }
        }

        public async Task<AgentToolResult> ValidateParametersAsync(AgentToolContext context)
        {
            var errors = new List<string>();

            if (!context.Parameters.ContainsKey("orderId"))
            {
                errors.Add("Order ID is required");
            }

            if (context.Parameters.TryGetValue("totalAmount", out var amountObj) && 
                !decimal.TryParse(amountObj.ToString(), out _))
            {
                errors.Add("Total amount must be a valid decimal");
            }

            if (errors.Any())
            {
                return new AgentToolResult
                {
                    Success = false,
                    Error = string.Join(", ", errors)
                };
            }

            return new AgentToolResult { Success = true };
        }
    }

    public class UpdateOrderAgentToolTests
    {
        [Fact]
        public async Task ExecuteAsync_ReturnsError_WhenOrderNotFound()
        {
            // Arrange
            var mockOrderService = new Mock<IOrderService>();
            var mockLogger = new Mock<ILogger<UpdateOrderAgentTool>>();
            var tool = new UpdateOrderAgentTool(mockOrderService.Object, mockLogger.Object);
            var context = new AgentToolContext 
            { 
                Parameters = new Dictionary<string, object> { ["orderId"] = "missing" } 
            };

            mockOrderService.Setup(s => s.GetOrderByIdAsync("missing")).ReturnsAsync((Order?)null);
            mockOrderService.Setup(s => s.GetOrderByNumberAsync("missing")).ReturnsAsync((Order?)null);

            // Act
            var result = await tool.ExecuteAsync(context);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Order not found", result.Error);
            mockOrderService.Verify(s => s.GetOrderByIdAsync("missing"), Times.Once);
            mockOrderService.Verify(s => s.GetOrderByNumberAsync("missing"), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsError_WhenOrderIdMissing()
        {
            // Arrange
            var mockOrderService = new Mock<IOrderService>();
            var mockLogger = new Mock<ILogger<UpdateOrderAgentTool>>();
            var tool = new UpdateOrderAgentTool(mockOrderService.Object, mockLogger.Object);
            var context = new AgentToolContext 
            { 
                Parameters = new Dictionary<string, object>() 
            };

            // Act
            var result = await tool.ExecuteAsync(context);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Order ID parameter is required", result.Error);
            mockOrderService.Verify(s => s.GetOrderByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ReturnsSuccess_WhenOrderUpdatedSuccessfully()
        {
            // Arrange
            var mockOrderService = new Mock<IOrderService>();
            var mockLogger = new Mock<ILogger<UpdateOrderAgentTool>>();
            var tool = new UpdateOrderAgentTool(mockOrderService.Object, mockLogger.Object);

            var existingOrder = new Order
            {
                Id = "order-123",
                OrderNumber = "ORD-001",
                Status = "pending",
                TotalAmount = 100.00m,
                CreatedAt = DateTime.UtcNow
            };

            var updatedOrder = new Order
            {
                Id = "order-123",
                OrderNumber = "ORD-001",
                Status = "completed",
                TotalAmount = 150.00m,
                CreatedAt = existingOrder.CreatedAt
            };

            var context = new AgentToolContext 
            { 
                Parameters = new Dictionary<string, object> 
                { 
                    ["orderId"] = "order-123",
                    ["status"] = "completed",
                    ["totalAmount"] = "150.00"
                } 
            };

            mockOrderService.Setup(s => s.GetOrderByIdAsync("order-123")).ReturnsAsync(existingOrder);
            mockOrderService.Setup(s => s.UpdateOrderAsync("order-123", It.IsAny<Order>())).ReturnsAsync(updatedOrder);

            // Act
            var result = await tool.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Order updated successfully", result.Message);
            Assert.NotNull(result.Data);
            mockOrderService.Verify(s => s.UpdateOrderAsync("order-123", It.IsAny<Order>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_HandlesException_AndReturnsError()
        {
            // Arrange
            var mockOrderService = new Mock<IOrderService>();
            var mockLogger = new Mock<ILogger<UpdateOrderAgentTool>>();
            var tool = new UpdateOrderAgentTool(mockOrderService.Object, mockLogger.Object);

            var context = new AgentToolContext 
            { 
                Parameters = new Dictionary<string, object> { ["orderId"] = "order-123" } 
            };

            mockOrderService.Setup(s => s.GetOrderByIdAsync("order-123"))
                           .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await tool.ExecuteAsync(context);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Error updating order", result.Error!);
            Assert.Contains("Database error", result.Error!);
        }

        [Fact]
        public async Task ValidateParametersAsync_ReturnsError_WhenOrderIdMissing()
        {
            // Arrange
            var mockOrderService = new Mock<IOrderService>();
            var mockLogger = new Mock<ILogger<UpdateOrderAgentTool>>();
            var tool = new UpdateOrderAgentTool(mockOrderService.Object, mockLogger.Object);

            var context = new AgentToolContext 
            { 
                Parameters = new Dictionary<string, object> { ["status"] = "completed" } 
            };

            // Act
            var result = await tool.ValidateParametersAsync(context);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Order ID is required", result.Error);
        }

        [Fact]
        public async Task ValidateParametersAsync_ReturnsError_WhenTotalAmountInvalid()
        {
            // Arrange
            var mockOrderService = new Mock<IOrderService>();
            var mockLogger = new Mock<ILogger<UpdateOrderAgentTool>>();
            var tool = new UpdateOrderAgentTool(mockOrderService.Object, mockLogger.Object);

            var context = new AgentToolContext 
            { 
                Parameters = new Dictionary<string, object> 
                { 
                    ["orderId"] = "order-123",
                    ["totalAmount"] = "invalid-amount"
                } 
            };

            // Act
            var result = await tool.ValidateParametersAsync(context);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Total amount must be a valid decimal", result.Error);
        }

        [Fact]
        public async Task ValidateParametersAsync_ReturnsSuccess_WhenParametersValid()
        {
            // Arrange
            var mockOrderService = new Mock<IOrderService>();
            var mockLogger = new Mock<ILogger<UpdateOrderAgentTool>>();
            var tool = new UpdateOrderAgentTool(mockOrderService.Object, mockLogger.Object);

            var context = new AgentToolContext 
            { 
                Parameters = new Dictionary<string, object> 
                { 
                    ["orderId"] = "order-123",
                    ["status"] = "completed",
                    ["totalAmount"] = "150.00"
                } 
            };

            // Act
            var result = await tool.ValidateParametersAsync(context);

            // Assert
            Assert.True(result.Success);
        }
    }
}
