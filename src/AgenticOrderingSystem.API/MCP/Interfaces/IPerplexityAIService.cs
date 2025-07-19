using AgenticOrderingSystem.API.MCP.Models;

namespace AgenticOrderingSystem.API.MCP.Interfaces
{
    /// <summary>
    /// Service for integrating with Perplexity AI API
    /// </summary>
    public interface IPerplexityAIService
    {
        /// <summary>
        /// Send a message to Perplexity AI and get a response
        /// </summary>
        /// <param name="request">AI request with message and context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>AI response</returns>
        Task<AIResponse> SendMessageAsync(AIRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyze an order rejection and provide insights
        /// </summary>
        /// <param name="orderId">Order ID to analyze</param>
        /// <param name="rejectionDetails">Rejection details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>AI analysis of the rejection</returns>
        Task<RejectionAnalysis> AnalyzeRejectionAsync(string orderId, object rejectionDetails, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate suggestions for fixing order issues
        /// </summary>
        /// <param name="orderId">Order ID</param>
        /// <param name="problemContext">Context about the problem</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>AI-generated suggestions</returns>
        Task<List<AIsuggestion>> GenerateSuggestionsAsync(string orderId, string problemContext, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get conversation response with tool usage
        /// </summary>
        /// <param name="sessionId">Session ID for context</param>
        /// <param name="message">User message</param>
        /// <param name="availableTools">Tools available for the AI to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>AI response with potential tool calls</returns>
        Task<ConversationResponse> GetConversationResponseAsync(string sessionId, string message, List<ToolSchema> availableTools, CancellationToken cancellationToken = default);
    }
}
