using AgenticOrderingSystem.API.MCP.Models;

namespace AgenticOrderingSystem.API.MCP.Interfaces
{
    /// <summary>
    /// Service for orchestrating MCP tools and managing tool execution
    /// </summary>
    public interface IMCPOrchestrator
    {
        /// <summary>
        /// Get all available tools
        /// </summary>
        /// <returns>List of available tools</returns>
        IEnumerable<IMCPTool> GetAvailableTools();

        /// <summary>
        /// Get a specific tool by name
        /// </summary>
        /// <param name="toolName">Name of the tool</param>
        /// <returns>Tool instance or null if not found</returns>
        IMCPTool? GetTool(string toolName);

        /// <summary>
        /// Execute a tool with parameters
        /// </summary>
        /// <param name="toolName">Name of the tool to execute</param>
        /// <param name="parameters">Parameters for the tool</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tool execution result</returns>
        Task<ToolResult> ExecuteToolAsync(string toolName, object parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute multiple tools in sequence
        /// </summary>
        /// <param name="toolCalls">List of tool calls to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of tool execution results</returns>
        Task<List<ToolResult>> ExecuteToolsAsync(List<ToolCall> toolCalls, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get tool schema for AI agent consumption
        /// </summary>
        /// <returns>Tool schemas for all available tools</returns>
        Task<List<ToolSchema>> GetToolSchemasAsync();
    }
}
