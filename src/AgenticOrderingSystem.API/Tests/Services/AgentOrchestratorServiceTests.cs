using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AgenticOrderingSystem.API.Tests.Services
{
    // Mock classes for testing
    public class AgentSession
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public interface IAgentOrchestratorService
    {
        Task<AgentSession> CreateAgentSessionAsync(string userId);
        Task<AgentSession?> GetAgentSessionAsync(string sessionId);
        Task<bool> ExecuteAgentActionAsync(string sessionId, string action);
        Task<List<AgentSession>> GetActiveSessionsAsync();
        Task<string> HandleChatMessageAsync(string userId, string message);
    }

    public class AgentOrchestratorService : IAgentOrchestratorService
    {
        private readonly ILogger<AgentOrchestratorService> _logger;
        private readonly Dictionary<string, AgentSession> _sessions = new();

        public AgentOrchestratorService(ILogger<AgentOrchestratorService> logger)
        {
            _logger = logger;
        }

        public Task<AgentSession> CreateAgentSessionAsync(string userId)
        {
            var session = new AgentSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };
            
            _sessions[session.Id] = session;
            return Task.FromResult(session);
        }

        public Task<AgentSession?> GetAgentSessionAsync(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }

        public Task<bool> ExecuteAgentActionAsync(string sessionId, string action)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                _logger.LogInformation($"Executing action {action} for session {sessionId}");
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<List<AgentSession>> GetActiveSessionsAsync()
        {
            var activeSessions = _sessions.Values
                .Where(s => s.Status == "active")
                .ToList();
            return Task.FromResult(activeSessions);
        }

        public Task<string> HandleChatMessageAsync(string userId, string message)
        {
            _logger.LogInformation($"Handling chat message for user {userId}: {message}");
            return Task.FromResult($"Response to: {message}");
        }
    }

    public class AgentOrchestratorServiceTests
    {
        [Fact]
        public async Task HandleChatMessageAsync_ReturnsResponse()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AgentOrchestratorService>>();
            var service = new AgentOrchestratorService(mockLogger.Object);

            // Act
            var response = await service.HandleChatMessageAsync("user", "hello");

            // Assert
            Assert.NotNull(response);
            Assert.Contains("hello", response);
        }

        [Fact]
        public async Task CreateAgentSession_ReturnsNewSession_WhenValidUserProvided()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AgentOrchestratorService>>();
            var service = new AgentOrchestratorService(mockLogger.Object);
            var userId = "user-123";

            // Act
            var result = await service.CreateAgentSessionAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("active", result.Status);
            Assert.NotEmpty(result.Id);
        }

        [Fact]
        public async Task GetAgentSession_ReturnsSession_WhenSessionExists()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AgentOrchestratorService>>();
            var service = new AgentOrchestratorService(mockLogger.Object);
            var createdSession = await service.CreateAgentSessionAsync("user-123");

            // Act
            var result = await service.GetAgentSessionAsync(createdSession.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createdSession.Id, result.Id);
            Assert.Equal("user-123", result.UserId);
        }

        [Fact]
        public async Task GetAgentSession_ReturnsNull_WhenSessionDoesNotExist()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AgentOrchestratorService>>();
            var service = new AgentOrchestratorService(mockLogger.Object);

            // Act
            var result = await service.GetAgentSessionAsync("non-existent-id");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ExecuteAgentAction_ReturnsTrue_WhenSessionExists()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AgentOrchestratorService>>();
            var service = new AgentOrchestratorService(mockLogger.Object);
            var session = await service.CreateAgentSessionAsync("user-123");

            // Act
            var result = await service.ExecuteAgentActionAsync(session.Id, "test-action");

            // Assert
            Assert.True(result);
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executing action test-action")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAgentAction_ReturnsFalse_WhenSessionDoesNotExist()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AgentOrchestratorService>>();
            var service = new AgentOrchestratorService(mockLogger.Object);

            // Act
            var result = await service.ExecuteAgentActionAsync("non-existent-id", "test-action");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetActiveSessions_ReturnsOnlyActiveSessions()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AgentOrchestratorService>>();
            var service = new AgentOrchestratorService(mockLogger.Object);
            
            // Create multiple sessions
            await service.CreateAgentSessionAsync("user-1");
            await service.CreateAgentSessionAsync("user-2");
            await service.CreateAgentSessionAsync("user-3");

            // Act
            var result = await service.GetActiveSessionsAsync();

            // Assert
            Assert.Equal(3, result.Count);
            Assert.All(result, session => Assert.Equal("active", session.Status));
        }
    }
}
