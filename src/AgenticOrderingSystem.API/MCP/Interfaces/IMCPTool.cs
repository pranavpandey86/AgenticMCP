using AgenticOrderingSystem.API.MCP.Models;

namespace AgenticOrderingSystem.API.MCP.Interfaces
{
    /// <summary>
    /// Interface for all MCP tools that can be executed by the AI agent
    /// </summary>
    public interface IMCPTool
    {
        /// <summary>
        /// Unique identifier for the tool
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Human-readable description of what the tool does
        /// </summary>
        string Description { get; }

        /// <summary>
        /// JSON schema defining the parameters this tool accepts
        /// </summary>
        ParameterSchema Parameters { get; }

        /// <summary>
        /// Execute the tool with the provided parameters
        /// </summary>
        /// <param name="parameters">Tool parameters as JSON object</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tool execution result</returns>
        Task<ToolResult> ExecuteAsync(object parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate parameters before execution
        /// </summary>
        /// <param name="parameters">Parameters to validate</param>
        /// <returns>Validation result</returns>
        ValidationResult ValidateParameters(object parameters);
    }
}
