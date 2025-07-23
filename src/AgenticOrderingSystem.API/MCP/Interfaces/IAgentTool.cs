using System.Text.Json;

namespace AgenticOrderingSystem.API.MCP.Interfaces;

/// <summary>
/// Interface for agent tools that can be executed by the orchestrator
/// </summary>
public interface IAgentTool
{
    /// <summary>
    /// Name of the tool for identification
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Description of what the tool does
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Execute the tool with given context
    /// </summary>
    Task<AgentToolResult> ExecuteAsync(AgentToolContext context);
}

/// <summary>
/// Context provided to agent tools during execution
/// </summary>
public class AgentToolContext
{
    public string UserId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string ConversationHistory { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
}

/// <summary>
/// Result returned by agent tools
/// </summary>
public class AgentToolResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public string? Error { get; set; }
    public AgentAction? NextAction { get; set; }
}

/// <summary>
/// Suggested next action for the agent
/// </summary>
public class AgentAction
{
    public string Type { get; set; } = string.Empty; // "prompt_user", "call_tool", "complete"
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}
