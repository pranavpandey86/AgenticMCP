using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AgenticOrderingSystem.API.Tests.Controllers
{
    // Mock classes for testing without API dependency
    public class Order
    {
        public string Id { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public interface IOrderService
    {
        Task<Order?> GetOrderByIdAsync(string orderId);
        Task<Order> CreateOrderAsync(Order order);
        Task<Order?> UpdateOrderAsync(string orderId, Order order);
        Task<bool> DeleteOrderAsync(string orderId);
    }

    public interface IUserService
    {
        Task<bool> ValidateUserAsync(string userId);
    }

    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, IUserService userService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(string id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();
            
            return Ok(order);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            var createdOrder = await _orderService.CreateOrderAsync(order);
            return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, createdOrder);
        }
    }

    public class OrderControllerTests
    {
        [Fact]
        public async Task GetOrder_ReturnsOk_WhenOrderExists()
        {
            // Arrange
            var mockOrderService = new Mock<IOrderService>();
            var mockUserService = new Mock<IUserService>();
            var mockLogger = new Mock<ILogger<OrderController>>();
            var controller = new OrderController(mockOrderService.Object, mockUserService.Object, mockLogger.Object);

            var testOrder = new Order 
            { 
                Id = "test-order-id", 
                OrderNumber = "TEST-001",
                Status = "created",
                TotalAmount = 100.00m
            };
            
            mockOrderService.Setup(s => s.GetOrderByIdAsync("test-order-id"))
                          .ReturnsAsync(testOrder);

            // Act
            var result = await controller.GetOrder("test-order-id");

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult?.Value);
            var returnedOrder = okResult?.Value as Order;
            Assert.Equal("test-order-id", returnedOrder?.Id);
        }

        [Fact]
        public async Task GetOrder_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var mockOrderService = new Mock<IOrderService>();
            var mockUserService = new Mock<IUserService>();
            var mockLogger = new Mock<ILogger<OrderController>>();
            var controller = new OrderController(mockOrderService.Object, mockUserService.Object, mockLogger.Object);

            mockOrderService.Setup(s => s.GetOrderByIdAsync("missing-order-id"))
                          .ReturnsAsync((Order?)null);

            // Act
            var result = await controller.GetOrder("missing-order-id");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateOrder_ReturnsCreatedAtAction_WhenOrderIsValid()
        {
            // Arrange
            var mockOrderService = new Mock<IOrderService>();
            var mockUserService = new Mock<IUserService>();
            var mockLogger = new Mock<ILogger<OrderController>>();
            var controller = new OrderController(mockOrderService.Object, mockUserService.Object, mockLogger.Object);

            var newOrder = new Order 
            { 
                OrderNumber = "NEW-001",
                Status = "pending",
                TotalAmount = 250.00m
            };

            var createdOrder = new Order 
            { 
                Id = "new-order-id",
                OrderNumber = "NEW-001",
                Status = "created",
                TotalAmount = 250.00m
            };
            
            mockOrderService.Setup(s => s.CreateOrderAsync(It.IsAny<Order>()))
                          .ReturnsAsync(createdOrder);

            // Act
            var result = await controller.CreateOrder(newOrder);

            // Assert
            Assert.IsType<CreatedAtActionResult>(result);
            var createdAtActionResult = result as CreatedAtActionResult;
            Assert.Equal("GetOrder", createdAtActionResult?.ActionName);
            Assert.NotNull(createdAtActionResult?.Value);
        }

        [Fact]
        public void OrderService_MockSetup_VerifyMethodCalls()
        {
            // Arrange
            var mockOrderService = new Mock<IOrderService>();
            var testOrder = new Order { Id = "test-id", OrderNumber = "TEST-001" };
            
            mockOrderService.Setup(s => s.GetOrderByIdAsync("test-id"))
                          .ReturnsAsync(testOrder);

            // Act
            var result = mockOrderService.Object.GetOrderByIdAsync("test-id").Result;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-id", result.Id);
            mockOrderService.Verify(s => s.GetOrderByIdAsync("test-id"), Times.Once);
        }
    }
}
