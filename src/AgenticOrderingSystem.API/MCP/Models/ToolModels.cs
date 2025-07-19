using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AgenticOrderingSystem.API.MCP.Models
{
    /// <summary>
    /// Represents the result of a tool execution
    /// </summary>
    public class ToolResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public object? Data { get; set; }

        [JsonPropertyName("error")]
        public ToolError? Error { get; set; }

        [JsonPropertyName("metadata")]
        public ToolMetadata Metadata { get; set; } = new();

        public static ToolResult CreateSuccess(object? data = null, ToolMetadata? metadata = null)
        {
            return new ToolResult
            {
                Success = true,
                Data = data,
                Metadata = metadata ?? new ToolMetadata()
            };
        }

        public static ToolResult CreateError(string code, string message, object? details = null)
        {
            return new ToolResult
            {
                Success = false,
                Error = new ToolError
                {
                    Code = code,
                    Message = message,
                    Details = details
                },
                Metadata = new ToolMetadata()
            };
        }
    }

    /// <summary>
    /// Error information for tool execution failures
    /// </summary>
    public class ToolError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public object? Details { get; set; }
    }

    /// <summary>
    /// Metadata about tool execution
    /// </summary>
    public class ToolMetadata
    {
        [JsonPropertyName("executionTime")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("toolVersion")]
        public string ToolVersion { get; set; } = "1.0.0";

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Schema definition for tool parameters
    /// </summary>
    public class ParameterSchema
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonPropertyName("properties")]
        public Dictionary<string, ParameterProperty> Properties { get; set; } = new();

        [JsonPropertyName("required")]
        public List<string> Required { get; set; } = new();

        [JsonPropertyName("additionalProperties")]
        public bool AdditionalProperties { get; set; } = false;
    }

    /// <summary>
    /// Property definition within parameter schema
    /// </summary>
    public class ParameterProperty
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("required")]
        public bool Required { get; set; }

        [JsonPropertyName("enum")]
        public List<object>? Enum { get; set; }

        [JsonPropertyName("pattern")]
        public string? Pattern { get; set; }

        [JsonPropertyName("minimum")]
        public double? Minimum { get; set; }

        [JsonPropertyName("maximum")]
        public double? Maximum { get; set; }

        [JsonPropertyName("minLength")]
        public int? MinLength { get; set; }

        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("default")]
        public object? Default { get; set; }
    }

    /// <summary>
    /// Validation result for parameter validation
    /// </summary>
    public class ValidationResult
    {
        [JsonPropertyName("isValid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("errors")]
        public List<ValidationError> Errors { get; set; } = new();

        [JsonPropertyName("warnings")]
        public List<ValidationWarning> Warnings { get; set; } = new();

        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        public static ValidationResult Failure(params ValidationError[] errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors.ToList()
            };
        }
    }

    /// <summary>
    /// Validation error details
    /// </summary>
    public class ValidationError
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }

    /// <summary>
    /// Validation warning details
    /// </summary>
    public class ValidationWarning
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("suggestion")]
        public string? Suggestion { get; set; }
    }

    /// <summary>
    /// Represents a tool call request
    /// </summary>
    public class ToolCall
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("toolName")]
        [Required]
        public string ToolName { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public object Parameters { get; set; } = new();

        [JsonPropertyName("timeout")]
        public int TimeoutMs { get; set; } = 30000;
    }

    /// <summary>
    /// Schema for tools available to AI agents
    /// </summary>
    public class ToolSchema
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public ParameterSchema Parameters { get; set; } = new();

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("examples")]
        public List<ToolExample>? Examples { get; set; }
    }

    /// <summary>
    /// Example usage of a tool
    /// </summary>
    public class ToolExample
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("parameters")]
        public object Parameters { get; set; } = new();

        [JsonPropertyName("expectedResult")]
        public object? ExpectedResult { get; set; }
    }
}
