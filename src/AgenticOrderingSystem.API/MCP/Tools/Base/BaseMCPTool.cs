using AgenticOrderingSystem.API.MCP.Interfaces;
using AgenticOrderingSystem.API.MCP.Models;
using System.Diagnostics;
using System.Text.Json;

namespace AgenticOrderingSystem.API.MCP.Tools.Base
{
    /// <summary>
    /// Base class for all MCP tools providing common functionality
    /// </summary>
    public abstract class BaseMCPTool : IMCPTool
    {
        protected readonly ILogger _logger;

        protected BaseMCPTool(ILogger logger)
        {
            _logger = logger;
        }

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract ParameterSchema Parameters { get; }

        public virtual async Task<ToolResult> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();

            try
            {
                _logger.LogInformation("Executing tool {ToolName} with request ID {RequestId}", Name, requestId);

                // Validate parameters
                var validationResult = ValidateParameters(parameters);
                if (!validationResult.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Errors.Select(e => e.Message));
                    return ToolResult.CreateError("VALIDATION_FAILED", $"Parameter validation failed: {errors}");
                }

                // Execute the tool-specific logic
                var result = await ExecuteInternalAsync(parameters, cancellationToken);

                // Set metadata
                result.Metadata.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                result.Metadata.RequestId = requestId;

                _logger.LogInformation("Tool {ToolName} executed successfully in {ExecutionTime}ms", Name, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Tool {ToolName} execution was cancelled", Name);
                return ToolResult.CreateError("OPERATION_CANCELLED", "Tool execution was cancelled");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error executing tool {ToolName}", Name);
                
                return ToolResult.CreateError("EXECUTION_ERROR", ex.Message, new
                {
                    ToolName = Name,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    RequestId = requestId
                });
            }
        }

        public virtual ValidationResult ValidateParameters(object parameters)
        {
            try
            {
                if (parameters == null)
                {
                    return ValidationResult.Failure(new ValidationError
                    {
                        Field = "parameters",
                        Code = "NULL_PARAMETERS",
                        Message = "Parameters cannot be null"
                    });
                }

                // Convert to JSON and back to validate structure
                var json = JsonSerializer.Serialize(parameters);
                var deserializedParams = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (deserializedParams == null)
                {
                    return ValidationResult.Failure(new ValidationError
                    {
                        Field = "parameters",
                        Code = "INVALID_JSON",
                        Message = "Parameters must be a valid JSON object"
                    });
                }

                // Validate required parameters
                var errors = new List<ValidationError>();
                foreach (var requiredField in Parameters.Required)
                {
                    if (!deserializedParams.ContainsKey(requiredField) || deserializedParams[requiredField] == null)
                    {
                        errors.Add(new ValidationError
                        {
                            Field = requiredField,
                            Code = "REQUIRED_FIELD_MISSING",
                            Message = $"Required field '{requiredField}' is missing or null"
                        });
                    }
                }

                // Validate field types and constraints
                foreach (var kvp in deserializedParams)
                {
                    if (Parameters.Properties.TryGetValue(kvp.Key, out var property))
                    {
                        var fieldValidation = ValidateField(kvp.Key, kvp.Value, property);
                        if (!fieldValidation.IsValid)
                        {
                            errors.AddRange(fieldValidation.Errors);
                        }
                    }
                }

                if (errors.Any())
                {
                    return ValidationResult.Failure(errors.ToArray());
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating parameters for tool {ToolName}", Name);
                return ValidationResult.Failure(new ValidationError
                {
                    Field = "parameters",
                    Code = "VALIDATION_ERROR",
                    Message = $"Parameter validation error: {ex.Message}"
                });
            }
        }

        protected virtual ValidationResult ValidateField(string fieldName, object? value, ParameterProperty property)
        {
            var errors = new List<ValidationError>();

            if (value == null && property.Required)
            {
                errors.Add(new ValidationError
                {
                    Field = fieldName,
                    Code = "REQUIRED_FIELD_NULL",
                    Message = $"Required field '{fieldName}' cannot be null"
                });
                return ValidationResult.Failure(errors.ToArray());
            }

            if (value == null)
            {
                return ValidationResult.Success();
            }

            // Type validation
            switch (property.Type.ToLowerInvariant())
            {
                case "string":
                    var strValue = ExtractStringValue(value);
                    if (strValue == null)
                    {
                        errors.Add(new ValidationError
                        {
                            Field = fieldName,
                            Code = "INVALID_TYPE",
                            Message = $"Field '{fieldName}' must be a string",
                            Value = value
                        });
                    }
                    else
                    {
                        ValidateStringConstraints(fieldName, strValue, property, errors);
                    }
                    break;

                case "number":
                case "integer":
                    var numValue = ExtractNumericValue(value);
                    if (numValue == null)
                    {
                        errors.Add(new ValidationError
                        {
                            Field = fieldName,
                            Code = "INVALID_TYPE",
                            Message = $"Field '{fieldName}' must be a number",
                            Value = value
                        });
                    }
                    else
                    {
                        ValidateNumericConstraints(fieldName, numValue.Value, property, errors);
                    }
                    break;

                case "boolean":
                    var boolValue = ExtractBooleanValue(value);
                    if (boolValue == null)
                    {
                        errors.Add(new ValidationError
                        {
                            Field = fieldName,
                            Code = "INVALID_TYPE",
                            Message = $"Field '{fieldName}' must be a boolean",
                            Value = value
                        });
                    }
                    break;
            }

            // Enum validation
            if (property.Enum != null && property.Enum.Any())
            {
                if (!property.Enum.Contains(value))
                {
                    errors.Add(new ValidationError
                    {
                        Field = fieldName,
                        Code = "INVALID_ENUM_VALUE",
                        Message = $"Field '{fieldName}' must be one of: {string.Join(", ", property.Enum)}",
                        Value = value
                    });
                }
            }

            return errors.Any() ? ValidationResult.Failure(errors.ToArray()) : ValidationResult.Success();
        }

        private void ValidateStringConstraints(string fieldName, string value, ParameterProperty property, List<ValidationError> errors)
        {
            if (property.MinLength.HasValue && value.Length < property.MinLength.Value)
            {
                errors.Add(new ValidationError
                {
                    Field = fieldName,
                    Code = "MIN_LENGTH_VIOLATION",
                    Message = $"Field '{fieldName}' must be at least {property.MinLength.Value} characters long",
                    Value = value
                });
            }

            if (property.MaxLength.HasValue && value.Length > property.MaxLength.Value)
            {
                errors.Add(new ValidationError
                {
                    Field = fieldName,
                    Code = "MAX_LENGTH_VIOLATION",
                    Message = $"Field '{fieldName}' must not exceed {property.MaxLength.Value} characters",
                    Value = value
                });
            }

            if (!string.IsNullOrEmpty(property.Pattern))
            {
                try
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(value, property.Pattern))
                    {
                        errors.Add(new ValidationError
                        {
                            Field = fieldName,
                            Code = "PATTERN_MISMATCH",
                            Message = $"Field '{fieldName}' does not match the required pattern",
                            Value = value
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid regex pattern for field {FieldName}: {Pattern}", fieldName, property.Pattern);
                }
            }
        }

        private void ValidateNumericConstraints(string fieldName, double value, ParameterProperty property, List<ValidationError> errors)
        {
            if (property.Minimum.HasValue && value < property.Minimum.Value)
            {
                errors.Add(new ValidationError
                {
                    Field = fieldName,
                    Code = "MINIMUM_VIOLATION",
                    Message = $"Field '{fieldName}' must be at least {property.Minimum.Value}",
                    Value = value
                });
            }

            if (property.Maximum.HasValue && value > property.Maximum.Value)
            {
                errors.Add(new ValidationError
                {
                    Field = fieldName,
                    Code = "MAXIMUM_VIOLATION",
                    Message = $"Field '{fieldName}' must not exceed {property.Maximum.Value}",
                    Value = value
                });
            }
        }

        private static bool IsNumeric(object value)
        {
            return value is int || value is long || value is float || value is double || value is decimal ||
                   (value is string str && double.TryParse(str, out _));
        }

        private static string? ExtractStringValue(object? value)
        {
            if (value is string str) return str;
            if (value is JsonElement element && element.ValueKind == JsonValueKind.String) 
                return element.GetString();
            return value?.ToString();
        }

        private static double? ExtractNumericValue(object? value)
        {
            if (value is int i) return i;
            if (value is long l) return l;
            if (value is float f) return f;
            if (value is double d) return d;
            if (value is decimal dec) return (double)dec;
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var num))
                    return num;
            }
            if (value is string str && double.TryParse(str, out var parsed))
                return parsed;
            return null;
        }

        private static bool? ExtractBooleanValue(object? value)
        {
            if (value is bool b) return b;
            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.True) return true;
                if (element.ValueKind == JsonValueKind.False) return false;
            }
            if (value is string str)
            {
                if (bool.TryParse(str, out var parsed)) return parsed;
                if (str.Equals("1") || str.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
                if (str.Equals("0") || str.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            }
            return null;
        }

        /// <summary>
        /// Template method for tool-specific execution logic
        /// </summary>
        /// <param name="parameters">Validated parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tool execution result</returns>
        protected abstract Task<ToolResult> ExecuteInternalAsync(object parameters, CancellationToken cancellationToken);
    }
}
