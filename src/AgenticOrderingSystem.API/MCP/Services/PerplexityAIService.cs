using AgenticOrderingSystem.API.MCP.Interfaces;
using AgenticOrderingSystem.API.MCP.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgenticOrderingSystem.API.MCP.Services
{
    /// <summary>
    /// Service for integrating with Perplexity AI API
    /// </summary>
    public class PerplexityAIService : IPerplexityAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PerplexityAIService> _logger;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public PerplexityAIService(HttpClient httpClient, ILogger<PerplexityAIService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            _apiKey = Environment.GetEnvironmentVariable("PERPLEXITY_API_KEY") 
                ?? configuration["PerplexityAI:ApiKey"] 
                ?? throw new InvalidOperationException("Perplexity API key not configured");
            
            _baseUrl = Environment.GetEnvironmentVariable("PERPLEXITY_BASE_URL") 
                ?? configuration["PerplexityAI:BaseUrl"] 
                ?? "https://api.perplexity.ai";

            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AgenticOrderingSystem/1.0");
        }

        public async Task<AIResponse> SendMessageAsync(AIRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sending message to Perplexity AI for session {SessionId}", request.SessionId);

                var perplexityRequest = new
                {
                    model = "sonar-reasoning",
                    messages = BuildMessagesFromRequest(request),
                    max_tokens = request.MaxTokens ?? 2000,
                    temperature = request.Temperature ?? 0.3,
                    stream = false
                };

                var json = JsonSerializer.Serialize(perplexityRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Perplexity API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    
                    return new AIResponse
                    {
                        Message = $"API Error {response.StatusCode}: {errorContent}",
                        Confidence = 0.0,
                        Metadata = new AIResponseMetadata
                        {
                            ProcessingTimeMs = 0,
                            Model = "error",
                            RequestId = Guid.NewGuid().ToString()
                        }
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Perplexity API response: {Response}", responseContent);
                
                var perplexityResponse = JsonSerializer.Deserialize<PerplexityResponse>(responseContent);

                if (perplexityResponse?.Choices?.Any() != true)
                {
                    return new AIResponse
                    {
                        Message = "No response generated from AI",
                        Confidence = 0.0,
                        Metadata = new AIResponseMetadata
                        {
                            ProcessingTimeMs = 0,
                            Model = "sonar",
                            RequestId = Guid.NewGuid().ToString()
                        }
                    };
                }

                var choice = perplexityResponse.Choices.First();
                var aiResponse = new AIResponse
                {
                    Message = choice.Message?.Content ?? "No response generated",
                    Confidence = 0.8, // Default confidence, could be enhanced
                    DetectedIntent = ExtractIntent(choice.Message?.Content ?? ""),
                    ToolCalls = ExtractToolCalls(choice.Message?.Content ?? ""),
                    Metadata = new AIResponseMetadata
                    {
                        ProcessingTimeMs = 0, // Could calculate from stopwatch
                        Model = "sonar",
                        TokensUsed = new TokenUsage
                        {
                            TotalTokens = perplexityResponse.Usage?.TotalTokens ?? 0,
                            InputTokens = perplexityResponse.Usage?.PromptTokens ?? 0,
                            OutputTokens = perplexityResponse.Usage?.CompletionTokens ?? 0
                        },
                        EstimatedCost = (double?)CalculateCost(perplexityResponse.Usage?.TotalTokens ?? 0),
                        RequestId = Guid.NewGuid().ToString()
                    }
                };

                _logger.LogInformation("Successfully processed AI response for session {SessionId}", request.SessionId);
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Perplexity AI request for session {SessionId}", request.SessionId);
                
                return new AIResponse
                {
                    Message = $"Processing error: {ex.Message}",
                    Confidence = 0.0,
                    Metadata = new AIResponseMetadata
                    {
                        ProcessingTimeMs = 0,
                        Model = "error",
                        RequestId = Guid.NewGuid().ToString()
                    }
                };
            }
        }

        public async Task<ConversationResponse> GetConversationResponseAsync(
            string sessionId, 
            string message, 
            List<ToolSchema> availableTools, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting conversation response for session {SessionId}", sessionId);

                // Build enhanced message with available tools context
                var toolsDescription = BuildToolsDescription(availableTools);
                var enhancedMessage = $@"
SYSTEM CONTEXT: You are an AI assistant for an ordering system. You have access to these MCP tools:

{toolsDescription}

REASONING APPROACH (for sonar-reasoning model):
1. ANALYZE the user's request to understand what they need
2. IDENTIFY the appropriate tool based on these patterns:
   - Order details/status → get_order_details
   - User order history → get_user_orders  
   - Failure analysis/rejection reasons → analyze_order_failures
3. EXECUTE the tool with proper parameters
4. RESPOND with the actual data from the tool

FAILURE ANALYSIS KEYWORDS that trigger 'analyze_order_failures':
- ""failure"", ""reject"", ""denied"", ""why"", ""reason""
- ""analyze"", ""pattern"", ""common"", ""success factor""
- ""recommendation"", ""improve"", ""approval rate""

TOOL CALL FORMAT (exact syntax required):
TOOL_CALL: {{""toolName"": ""analyze_order_failures"", ""parameters"": {{""analysisType"": ""all"", ""timeRange"": ""quarter""}}}}

USER MESSAGE: {message}

Think step-by-step and use the appropriate tool to get real data.";

                var request = new AIRequest
                {
                    SessionId = sessionId,
                    Message = enhancedMessage,
                    Context = new AIContext
                    {
                        SessionData = new Dictionary<string, object>
                        {
                            ["availableTools"] = availableTools
                        }
                    }
                };

                var aiResponse = await SendMessageAsync(request, cancellationToken);

                return new ConversationResponse
                {
                    Message = aiResponse.Message,
                    ToolCalls = aiResponse.ToolCalls ?? new List<ToolCall>(),
                    Confidence = aiResponse.Confidence,
                    DetectedIntent = aiResponse.DetectedIntent,
                    Metadata = aiResponse.Metadata
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation response for session {SessionId}", sessionId);
                
                return new ConversationResponse
                {
                    Message = "I apologize, but I'm having trouble processing your request right now. Please try again later.",
                    ToolCalls = new List<ToolCall>(),
                    Confidence = 0.0,
                    Metadata = new AIResponseMetadata
                    {
                        ProcessingTimeMs = 0,
                        Model = "error",
                        RequestId = Guid.NewGuid().ToString()
                    }
                };
            }
        }

        public async Task<RejectionAnalysis> AnalyzeRejectionAsync(string orderId, object rejectionDetails, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Analyzing rejection for order {OrderId}", orderId);

                var analysisPrompt = $@"
Analyze this order rejection and provide insights:

Order ID: {orderId}
Rejection Details: {JsonSerializer.Serialize(rejectionDetails)}

Please provide:
1. Primary reasons for rejection
2. Specific issues that need to be addressed
3. Recommendations for improvement
4. Likelihood of approval if changes are made
";

                var request = new AIRequest
                {
                    SessionId = $"rejection-analysis-{orderId}",
                    Message = analysisPrompt,
                    Context = new AIContext
                    {
                        OrderId = orderId
                    }
                };

                var response = await SendMessageAsync(request, cancellationToken);

                return new RejectionAnalysis
                {
                    OrderId = orderId,
                    OverallReason = ExtractOverallReason(response.Message),
                    SpecificIssues = ExtractSpecificIssues(response.Message),
                    Suggestions = ExtractSuggestionsFromAnalysis(response.Message, orderId),
                    Confidence = response.Confidence,
                    EstimatedFixTimeMinutes = 30 // Default estimate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing rejection for order {OrderId}", orderId);
                
                return new RejectionAnalysis
                {
                    OrderId = orderId,
                    OverallReason = "Unable to analyze rejection at this time",
                    SpecificIssues = new List<SpecificIssue>(),
                    Suggestions = new List<AIsuggestion>
                    {
                        new() 
                        { 
                            Title = "Manual Review Required",
                            Description = "Please review rejection details manually",
                            Action = "Review and revise order details"
                        }
                    },
                    Confidence = 0.0,
                    EstimatedFixTimeMinutes = 60
                };
            }
        }

        public async Task<List<AIsuggestion>> GenerateSuggestionsAsync(string orderId, string problemContext, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Generating suggestions for order {OrderId}", orderId);

                var suggestionPrompt = $@"
Generate specific, actionable suggestions to improve this order:

Order ID: {orderId}
Problem Context: {problemContext}

Please provide 3-5 specific suggestions that would help get this order approved, ranked by priority.
Format each suggestion clearly with the action needed and expected outcome.
";

                var request = new AIRequest
                {
                    SessionId = $"suggestions-{orderId}",
                    Message = suggestionPrompt,
                    Context = new AIContext
                    {
                        OrderId = orderId
                    }
                };

                var response = await SendMessageAsync(request, cancellationToken);

                return ExtractSuggestionsFromResponse(response.Message, orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating suggestions for order {OrderId}", orderId);
                
                return new List<AIsuggestion>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "Review Required",
                        Description = "Unable to generate suggestions at this time. Please review the order manually.",
                        Action = "Manual review required",
                        Priority = "medium",
                        EstimatedImpact = "Unknown"
                    }
                };
            }
        }

        private List<PerplexityMessage> BuildMessagesFromRequest(AIRequest request)
        {
            var messages = new List<PerplexityMessage>();

            // Add conversation history if available
            if (request.Context?.ConversationHistory?.Any() == true)
            {
                foreach (var historyMessage in request.Context.ConversationHistory)
                {
                    messages.Add(new PerplexityMessage
                    {
                        Role = historyMessage.Sender == "user" ? "user" : "assistant",
                        Content = historyMessage.Message
                    });
                }
            }

            // Add current message
            messages.Add(new PerplexityMessage
            {
                Role = "user",
                Content = request.Message
            });

            return messages;
        }

        private string BuildToolsDescription(List<ToolSchema> availableTools)
        {
            if (availableTools?.Any() != true)
                return "No tools currently available.";

            var toolDescriptions = availableTools.Select(tool => 
                $"- **{tool.Name}**: {tool.Description}\n  Category: {tool.Category}\n  Parameters: {string.Join(", ", tool.Parameters?.Properties?.Keys?.ToList() ?? new List<string>())}"
            );

            return $"Available MCP Tools:\n{string.Join("\n\n", toolDescriptions)}";
        }

        private string? ExtractIntent(string response)
        {
            // Simple intent detection - could be enhanced with more sophisticated NLP
            var lowerResponse = response.ToLowerInvariant();
            
            if (lowerResponse.Contains("order") && lowerResponse.Contains("create"))
                return "create_order";
            if (lowerResponse.Contains("order") && lowerResponse.Contains("status"))
                return "check_order_status";
            if (lowerResponse.Contains("help") || lowerResponse.Contains("assistance"))
                return "help_request";
            
            return null;
        }

        private List<ToolCall>? ExtractToolCalls(string response)
        {
            var toolCalls = new List<ToolCall>();
            
            try
            {
                // Clean up the response - remove line breaks and extra spaces within JSON
                var cleanResponse = response.Replace("\n", " ").Replace("\r", " ");
                
                // Look for TOOL_CALL: pattern - more flexible regex that handles nested braces
                var regex = new System.Text.RegularExpressions.Regex(@"TOOL_CALL:\s*(\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var matches = regex.Matches(cleanResponse);
                
                _logger.LogInformation("Extracting tool calls from response. Found {MatchCount} matches in cleaned response: {Response}", matches.Count, cleanResponse);
                
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var jsonStr = match.Groups[1].Value.Trim();
                    _logger.LogInformation("Attempting to parse tool call JSON: {Json}", jsonStr);
                    
                    try
                    {
                        var toolCallData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonStr);
                        if (toolCallData != null && toolCallData.ContainsKey("toolName") && toolCallData.ContainsKey("parameters"))
                        {
                            var toolName = toolCallData["toolName"]?.ToString();
                            if (!string.IsNullOrEmpty(toolName))
                            {
                                var toolCall = new ToolCall
                                {
                                    ToolName = toolName,
                                    Parameters = toolCallData["parameters"]
                                };
                                toolCalls.Add(toolCall);
                                _logger.LogInformation("Successfully extracted tool call: {ToolName}", toolName);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning("Failed to parse tool call JSON: {Json} - {Error}", jsonStr, ex.Message);
                        
                        // Try manual parsing as fallback
                        try
                        {
                            var toolCall = ParseToolCallManually(jsonStr);
                            if (toolCall != null)
                            {
                                toolCalls.Add(toolCall);
                                _logger.LogInformation("Successfully parsed tool call manually: {ToolName}", toolCall.ToolName);
                            }
                        }
                        catch (Exception manualEx)
                        {
                            _logger.LogWarning("Manual parsing also failed: {Error}", manualEx.Message);
                        }
                    }
                }
                
                return toolCalls.Any() ? toolCalls : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting tool calls from response");
                return null;
            }
        }

        private ToolCall? ParseToolCallManually(string jsonStr)
        {
            // Manual parsing for simple cases
            var toolNameMatch = System.Text.RegularExpressions.Regex.Match(jsonStr, @"""toolName"":\s*""([^""]+)""");
            if (!toolNameMatch.Success) return null;

            var toolName = toolNameMatch.Groups[1].Value;
            
            // Extract parameters section
            var paramsMatch = System.Text.RegularExpressions.Regex.Match(jsonStr, @"""parameters"":\s*(\{[^}]*\})");
            if (!paramsMatch.Success) return null;

            var paramsStr = paramsMatch.Groups[1].Value;
            
            try
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(paramsStr);
                if (parameters != null)
                {
                    return new ToolCall
                    {
                        ToolName = toolName,
                        Parameters = parameters
                    };
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private decimal? CalculateCost(int totalTokens)
        {
            // Perplexity pricing calculation - would need to be updated with actual rates
            const decimal costPerToken = 0.00002m; // Example rate
            return totalTokens * costPerToken;
        }

        private List<string> ExtractReasonsFromResponse(string response)
        {
            // Parse the AI response to extract rejection reasons
            // This is a simplified implementation
            var reasons = new List<string>();
            
            if (response.Contains("budget") || response.Contains("cost"))
                reasons.Add("Budget constraints");
            if (response.Contains("justification") || response.Contains("business case"))
                reasons.Add("Insufficient business justification");
            if (response.Contains("approval") || response.Contains("authority"))
                reasons.Add("Approval authority issues");
            
            return reasons.Any() ? reasons : new List<string> { "General rejection reasons identified" };
        }

        private List<string> ExtractRecommendationsFromResponse(string response)
        {
            // Parse the AI response to extract recommendations
            // This is a simplified implementation
            var recommendations = new List<string>();
            
            if (response.Contains("budget"))
                recommendations.Add("Provide detailed budget justification");
            if (response.Contains("business"))
                recommendations.Add("Strengthen business case with specific benefits");
            if (response.Contains("approval"))
                recommendations.Add("Consult with appropriate approval authority");
            
            return recommendations.Any() ? recommendations : new List<string> { "Review and revise order details" };
        }

        private string ExtractOverallReason(string response)
        {
            // Extract the overall rejection reason from AI response
            if (response.Contains("budget", StringComparison.OrdinalIgnoreCase))
                return "Budget constraints identified";
            if (response.Contains("business case", StringComparison.OrdinalIgnoreCase))
                return "Business justification insufficient";
            if (response.Contains("approval", StringComparison.OrdinalIgnoreCase))
                return "Approval process issues";
            
            return "General rejection analysis completed";
        }

        private List<SpecificIssue> ExtractSpecificIssues(string response)
        {
            // Extract specific issues from AI response
            var issues = new List<SpecificIssue>();
            
            if (response.Contains("budget", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new SpecificIssue
                {
                    QuestionKey = "budget_justification",
                    QuestionText = "Budget Justification",
                    IssueDescription = "Budget justification needs improvement",
                    SuggestedAction = "Provide detailed cost-benefit analysis",
                    Severity = "high",
                    Category = "business"
                });
            }
            
            return issues;
        }

        private List<AIsuggestion> ExtractSuggestionsFromAnalysis(string response, string orderId)
        {
            // Extract suggestions from AI analysis response
            var suggestions = new List<AIsuggestion>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "AI Analysis Suggestion",
                    Description = "Based on AI analysis: " + response.Substring(0, Math.Min(response.Length, 200)) + "...",
                    Action = "Review and implement suggested changes",
                    Priority = "high",
                    EstimatedImpact = "Medium"
                }
            };

            return suggestions;
        }

        private List<AIsuggestion> ExtractSuggestionsFromResponse(string response, string orderId)
        {
            // Parse the AI response to extract structured suggestions
            // This is a simplified implementation
            var suggestions = new List<AIsuggestion>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Review Order Details",
                    Description = "Based on AI analysis: " + response.Substring(0, Math.Min(response.Length, 200)) + "...",
                    Action = "Review and update order details",
                    Priority = "high",
                    EstimatedImpact = "Medium"
                }
            };

            return suggestions;
        }
    }

    // Perplexity API response models
    public class PerplexityResponse
    {
        [JsonPropertyName("choices")]
        public List<PerplexityChoice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public PerplexityUsage? Usage { get; set; }
    }

    public class PerplexityChoice
    {
        [JsonPropertyName("message")]
        public PerplexityMessage? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class PerplexityMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class PerplexityUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
