using AgenticOrderingSystem.API.MCP.Interfaces;
using AgenticOrderingSystem.API.MCP.Services;
using AgenticOrderingSystem.API.MCP.Prompts;
using AgenticOrderingSystem.API.MCP.Models;
using AgenticOrderingSystem.API.Services;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AgenticOrderingSystem.API.Services;

/// <summary>
/// Service that orchestrates agent conversations and tool execution
/// </summary>
public interface IAgentOrchestratorService
{
    Task<AgentResponse> HandleChatMessageAsync(string userId, string message, string? conversationId = null);
    Task<AgentResponse> HandleConfirmationAsync(string userId, string conversationId, bool confirmed);
}

/// <summary>
/// Orchestrates intelligent agent conversations using AI-driven tool selection
/// </summary>
public class AgentOrchestratorService : IAgentOrchestratorService
{
    private readonly IEnumerable<IAgentTool> _tools;
    private readonly IConversationStateService _conversationService;
    private readonly IPerplexityAIService _perplexityService;
    private readonly ILogger<AgentOrchestratorService> _logger;

    public AgentOrchestratorService(
        IEnumerable<IAgentTool> tools,
        IConversationStateService conversationService,
        IPerplexityAIService perplexityService,
        ILogger<AgentOrchestratorService> logger)
    {
        _tools = tools;
        _conversationService = conversationService;
        _perplexityService = perplexityService;
        _logger = logger;
    }

    public async Task<AgentResponse> HandleChatMessageAsync(string userId, string message, string? conversationId = null)
    {
        try
        {
            var conversation = await _conversationService.GetOrCreateConversationAsync(userId, conversationId);
            conversation.AddMessage("user", message);

            // Use AI to determine the next action and appropriate tool
            var action = await DecideNextActionAsync(userId, message, conversation);
            
            AgentResponse response;

            switch (action.Action.ToLowerInvariant())
            {
                case "get_order_details":
                    response = await HandleGetOrderDetails(userId, message, conversation, action.Parameters);
                    break;

                case "analyze_order_failures":
                    response = await HandleAnalyzeOrderFailures(userId, message, conversation, action.Parameters);
                    break;

                case "update_order":
                    response = await HandleUpdateOrder(userId, message, conversation, action.Parameters);
                    break;

                case "general_help":
                default:
                    response = await HandleGeneralHelp(userId, message, conversation);
                    break;
            }

            conversation.AddMessage("assistant", response.Message);
            await _conversationService.UpdateConversationAsync(conversation);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling chat message for user {UserId}", userId);
            return new AgentResponse
            {
                Message = "I encountered an error processing your request. Please try again.",
                ConversationId = conversationId ?? Guid.NewGuid().ToString(),
                RequiresConfirmation = false
            };
        }
    }

    public async Task<AgentResponse> HandleConfirmationAsync(string userId, string conversationId, bool confirmed)
    {
        var conversation = await _conversationService.GetConversationAsync(conversationId);
        if (conversation == null)
        {
            return new AgentResponse
            {
                Message = "I couldn't find that conversation. Please start a new request.",
                ConversationId = conversationId,
                RequiresConfirmation = false
            };
        }

        if (confirmed)
        {
            // Execute the pending action
            return new AgentResponse
            {
                Message = "Thank you for confirming! I've processed your request.",
                ConversationId = conversationId,
                RequiresConfirmation = false
            };
        }
        else
        {
            return new AgentResponse
            {
                Message = "Understood, I won't proceed with that action. Is there anything else I can help you with?",
                ConversationId = conversationId,
                RequiresConfirmation = false
            };
        }
    }

    private async Task<IntentResult> DecideNextActionAsync(string userId, string message, ConversationState conversation)
    {
        try
        {
            // Build context about available tools
            var toolsContext = BuildToolsContext();
            
            // Create the AI prompt for intent detection
            var prompt = $@"
SYSTEM: You are a JSON extraction AI. Extract order ID from user message and determine the appropriate action.

USER MESSAGE: ""{message}""

EXTRACTION RULES FOR ORDER ID:
1. Look for patterns like ""ordernumber - TEAM-SUCCESS-001"" and extract ""TEAM-SUCCESS-001""
2. Look for patterns like ""order TEAM-FAIL-001"" and extract ""TEAM-FAIL-001""
3. The order ID is usually in format: TEAM-SUCCESS-001, TEAM-FAIL-001, ABC-123, etc.
4. NEVER extract words like ""order"", ""ordernumber"", ""number"", ""id""

ACTION DETERMINATION:
- If user asks for ""details"", ""info"", ""information"" about an order → ""get_order_details""
- If user asks about ""rejected"", ""failed"", ""issues"", ""problems"", ""why"" with an order → ""analyze_order_failures""
- If user wants to ""update"", ""fix"", ""modify"", ""change"" an order → ""update_order""
- For other messages → ""general_help""

SPECIFIC EXAMPLES:
- ""ordernumber - TEAM-SUCCESS-001"" → {{""action"": ""get_order_details"", ""parameters"": {{""orderId"": ""TEAM-SUCCESS-001""}}, ""confidence"": 0.95}}
- ""why was TEAM-FAIL-001 rejected?"" → {{""action"": ""analyze_order_failures"", ""parameters"": {{""orderId"": ""TEAM-FAIL-001""}}, ""confidence"": 0.95}}
- ""get details for order TEAM-SUCCESS-001"" → {{""action"": ""get_order_details"", ""parameters"": {{""orderId"": ""TEAM-SUCCESS-001""}}, ""confidence"": 0.95}}

RESPONSE FORMAT (JSON ONLY):
{{""action"": ""[ACTION]"", ""parameters"": {{""orderId"": ""[EXTRACTED-ID]""}}, ""confidence"": 0.95}}

ANALYZE: ""{message}""
EXTRACT ORDER ID AND DETERMINE ACTION, RESPOND WITH JSON:";

            var aiResponse = await _perplexityService.SendMessageAsync(new AIRequest
            {
                SessionId = $"intent-detection-{userId}",
                Message = prompt,
                Context = new AIContext()
            });
            
            _logger.LogInformation("AI Response for intent detection: {Response}", aiResponse.Message);
            
            // Clean the AI response - sometimes it has extra text
            var cleanResponse = aiResponse.Message.Trim();
            
            // Try to extract JSON if the response has extra text
            var jsonMatch = Regex.Match(cleanResponse, @"\{.*\}", RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                cleanResponse = jsonMatch.Value;
                _logger.LogInformation("Extracted JSON from AI response: {CleanResponse}", cleanResponse);
            }
            
            // Parse the AI response
            var intentResult = JsonSerializer.Deserialize<IntentResult>(cleanResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Handle JsonElement values in parameters
            if (intentResult?.Parameters != null)
            {
                var convertedParams = new Dictionary<string, object>();
                foreach (var kvp in intentResult.Parameters)
                {
                    if (kvp.Value is JsonElement jsonElement)
                    {
                        convertedParams[kvp.Key] = jsonElement.ValueKind switch
                        {
                            JsonValueKind.String => jsonElement.GetString() ?? "",
                            JsonValueKind.Number => jsonElement.GetDecimal(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            _ => kvp.Value
                        };
                    }
                    else
                    {
                        convertedParams[kvp.Key] = kvp.Value;
                    }
                }
                intentResult.Parameters = convertedParams;
            }

            _logger.LogInformation("Parsed intent result - Action: {Action}, Parameters: {Parameters}", 
                intentResult?.Action, 
                JsonSerializer.Serialize(intentResult?.Parameters ?? new()));

            return intentResult ?? new IntentResult 
            { 
                Action = "general_help", 
                Parameters = new Dictionary<string, object>(),
                Confidence = 0.5m
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI intent detection, falling back to pattern matching for message: {Message}", message);
            return FallbackIntentDetection(message);
        }
    }

    private string BuildToolsContext()
    {
        var toolDescriptions = _tools.Select(tool => $"- {tool.Name}: {tool.Description}");
        return string.Join("\n", toolDescriptions);
    }

    private IntentResult FallbackIntentDetection(string message)
    {
        var lowerMessage = message.ToLowerInvariant();
        
        // Extract order ID/number patterns - more specific patterns
        var orderPatterns = new[]
        {
            @"ordernumber\s*-\s*([A-Z0-9\-]+)",         // "ordernumber - TEAM-SUCCESS-001"
            @"order\s+number\s*[:\-]?\s*([A-Z0-9\-]+)", // "order number TEAM-SUCCESS-001"
            @"order\s+([A-Z0-9\-]+)",                   // "order TEAM-SUCCESS-001"
            @"order\s*[:\-]\s*([A-Z0-9\-]+)",          // "order: TEAM-SUCCESS-001"
            @"([A-Z]+-[A-Z0-9\-]+)",                   // Direct pattern like TEAM-SUCCESS-001
        };

        string orderId = "";
        foreach (var pattern in orderPatterns)
        {
            var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups[1].Value.Length > 3) // Ensure it's not just "ID" or similar
            {
                orderId = match.Groups[1].Value;
                break;
            }
        }

        _logger.LogInformation("Fallback pattern matching - Message: '{Message}', Extracted OrderId: '{OrderId}'", message, orderId);

        if (lowerMessage.Contains("detail") && !string.IsNullOrEmpty(orderId))
        {
            return new IntentResult
            {
                Action = "get_order_details",
                Parameters = new Dictionary<string, object> { ["orderId"] = orderId },
                Confidence = 0.8m
            };
        }

        if ((lowerMessage.Contains("fail") || lowerMessage.Contains("reject")) && !string.IsNullOrEmpty(orderId))
        {
            return new IntentResult
            {
                Action = "analyze_order_failures",
                Parameters = new Dictionary<string, object> { ["orderId"] = orderId },
                Confidence = 0.8m
            };
        }

        return new IntentResult
        {
            Action = "general_help",
            Parameters = new Dictionary<string, object>(),
            Confidence = 0.6m
        };
    }

    private async Task<AgentResponse> HandleGetOrderDetails(string userId, string message, ConversationState conversation, Dictionary<string, object> parameters)
    {
        var getOrderTool = _tools.FirstOrDefault(t => t.Name == "get_order_details");
        if (getOrderTool == null)
        {
            return new AgentResponse
            {
                Message = "I don't have access to the order details tool right now. Please try again later.",
                ConversationId = conversation.Id,
                RequiresConfirmation = false
            };
        }

        if (!parameters.ContainsKey("orderId"))
        {
            return new AgentResponse
            {
                Message = "I need an order ID to get the details. Please provide an order number like TEAM-FAIL-001.",
                ConversationId = conversation.Id,
                RequiresConfirmation = false
            };
        }

        var context = new AgentToolContext
        {
            UserId = userId,
            ConversationId = conversation.Id,
            Parameters = parameters,
            UserMessage = message
        };

        var result = await getOrderTool.ExecuteAsync(context);
        
        if (!result.Success)
        {
            return new AgentResponse
            {
                Message = $"I couldn't retrieve the order details: {result.Error}",
                ConversationId = conversation.Id,
                RequiresConfirmation = false
            };
        }

        // Generate a clean, human-readable response about the order
        var orderId = parameters["orderId"]?.ToString() ?? "unknown";
        var orderResponse = GenerateOrderDetailsResponse(result.Data, orderId);

        return new AgentResponse
        {
            Message = orderResponse,
            ConversationId = conversation.Id,
            RequiresConfirmation = false,
            Data = result.Data
        };
    }

    private async Task<AgentResponse> HandleAnalyzeOrderFailures(string userId, string message, ConversationState conversation, Dictionary<string, object> parameters)
    {
        var analysisTool = _tools.FirstOrDefault(t => t.Name == "analyze_order_failures");
        if (analysisTool == null)
        {
            return new AgentResponse
            {
                Message = "I don't have access to the failure analysis tool right now.",
                ConversationId = conversation.Id,
                RequiresConfirmation = false
            };
        }

        var context = new AgentToolContext
        {
            UserId = userId,
            ConversationId = conversation.Id,
            Parameters = parameters,
            UserMessage = message
        };

        var result = await analysisTool.ExecuteAsync(context);
        
        _logger.LogInformation("Raw tool result - Success: {Success}, Data: {Data}", result.Success, JsonSerializer.Serialize(result.Data));
        
        if (!result.Success)
        {
            return new AgentResponse
            {
                Message = $"I couldn't analyze the order failures: {result.Error}",
                ConversationId = conversation.Id,
                RequiresConfirmation = false
            };
        }

        var analysisResponse = GenerateCleanAnalysisResponse(result, parameters);

        var finalResponse = new AgentResponse
        {
            Message = analysisResponse,
            ConversationId = conversation.Id,
            RequiresConfirmation = false,
            Data = result.Data
        };
        
        _logger.LogInformation("Final AgentResponse data: {Data}", JsonSerializer.Serialize(finalResponse.Data));
        
        return finalResponse;
    }

    private async Task<AgentResponse> HandleUpdateOrder(string userId, string message, ConversationState conversation, Dictionary<string, object> parameters)
    {
        var updateTool = _tools.FirstOrDefault(t => t.Name == "update_order");
        if (updateTool == null)
        {
            return new AgentResponse
            {
                Message = "I don't have access to the order update tool right now.",
                ConversationId = conversation.Id,
                RequiresConfirmation = false
            };
        }

        var context = new AgentToolContext
        {
            UserId = userId,
            ConversationId = conversation.Id,
            Parameters = parameters,
            UserMessage = message
        };

        var result = await updateTool.ExecuteAsync(context);
        
        if (!result.Success)
        {
            return new AgentResponse
            {
                Message = $"I couldn't update the order: {result.Error}",
                ConversationId = conversation.Id,
                RequiresConfirmation = false
            };
        }

        return new AgentResponse
        {
            Message = "I've successfully updated your order with the recommended changes. Your order has been resubmitted for approval.",
            ConversationId = conversation.Id,
            RequiresConfirmation = false,
            Data = result.Data
        };
    }

    private string GenerateOrderDetailsResponse(Dictionary<string, object> orderData, string orderId)
    {
        try
        {
            // Extract key information from the order data to create a human-readable response
            var response = $"Here are the details for order {orderId}:\n\n";
            
            if (orderData.ContainsKey("order"))
            {
                var orderValue = orderData["order"];
                Dictionary<string, object>? order = null;
                
                // Handle JsonElement which is common when deserializing JSON
                if (orderValue is JsonElement jsonElement)
                {
                    var jsonString = jsonElement.GetRawText();
                    order = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else
                {
                    order = orderValue as Dictionary<string, object>;
                }
                
                if (order != null)
                {
                    var status = order.GetValueOrDefault("status", "unknown").ToString();
                    var productName = order.GetValueOrDefault("productName", "").ToString();
                    var amount = order.GetValueOrDefault("totalAmount", "").ToString();
                    var currency = order.GetValueOrDefault("currency", "USD").ToString();
                    var businessJustification = order.GetValueOrDefault("businessJustification", "").ToString();
                    var userName = order.GetValueOrDefault("userName", "").ToString();
                    
                    response += $"User: {userName}\n";
                    response += $"Product: {productName}\n";
                    response += $"Amount: {currency} {amount}\n";
                    response += $"Status: {status.ToUpper()}\n";
                    response += $"Justification: {businessJustification}\n\n";
                    
                    if (status.ToLower() == "rejected" && orderData.ContainsKey("workflowAnalysis"))
                    {
                        response += "This order was rejected. Would you like me to analyze why it was rejected and suggest fixes?";
                    }
                    else if (status.ToLower() == "approved")
                    {
                        response += "This order has been approved and is ready for fulfillment!";
                    }
                    else if (status.ToLower() == "pending")
                    {
                        response += "This order is pending approval.";
                    }
                }
            }
            else
            {
                response += "Order data is available but couldn't parse the details format.";
            }
            
            return response;
        }
        catch (Exception ex)
        {
            return $"I found the order {orderId}, but had trouble formatting the details: {ex.Message}. The order data is available if you need specific information.";
        }
    }

    private string GenerateCleanAnalysisResponse(AgentToolResult result, Dictionary<string, object> parameters)
    {
        try
        {
            _logger.LogInformation("Analysis result data: {Data}", JsonSerializer.Serialize(result.Data));
            
            var response = "🔍 **Order Rejection Analysis**\n\n";
            
            // Extract the structured response data
            var data = result.Data;
            string failureReason = "Order was rejected";
            string teamSuccessInfo = "";
            string aiRecommendations = "";
            
            // Access failureAnalysis -> aiAnalysis for the detailed AI response
            if (data.ContainsKey("failureAnalysis"))
            {
                var failureAnalysis = data["failureAnalysis"];
                if (failureAnalysis is Dictionary<string, object> failureDict)
                {
                    if (failureDict.ContainsKey("aiAnalysis"))
                    {
                        aiRecommendations = failureDict["aiAnalysis"]?.ToString() ?? "";
                    }
                    if (failureDict.ContainsKey("commonFailureReasons"))
                    {
                        var reasons = failureDict["commonFailureReasons"];
                        if (reasons is List<object> reasonsList && reasonsList.Any())
                        {
                            failureReason = string.Join(", ", reasonsList.Select(r => r.ToString()));
                        }
                    }
                }
                else if (failureAnalysis is JsonElement failureElement)
                {
                    if (failureElement.TryGetProperty("aiAnalysis", out var aiAnalysisElement))
                    {
                        aiRecommendations = aiAnalysisElement.GetString() ?? "";
                    }
                    if (failureElement.TryGetProperty("commonFailureReasons", out var reasonsElement))
                    {
                        if (reasonsElement.ValueKind == JsonValueKind.Array)
                        {
                            var reasons = reasonsElement.EnumerateArray().Select(r => r.GetString()).Where(r => !string.IsNullOrEmpty(r));
                            if (reasons.Any())
                            {
                                failureReason = string.Join(", ", reasons);
                            }
                        }
                    }
                }
            }
            
            // Extract team success patterns
            if (data.ContainsKey("successAnalysis"))
            {
                var successAnalysis = data["successAnalysis"];
                if (successAnalysis is Dictionary<string, object> successDict)
                {
                    if (successDict.ContainsKey("teamPatterns"))
                    {
                        var patterns = successDict["teamPatterns"];
                        if (patterns is List<object> patternsList && patternsList.Any())
                        {
                            var firstPattern = patternsList.First() as Dictionary<string, object>;
                            if (firstPattern != null && firstPattern.ContainsKey("product"))
                            {
                                teamSuccessInfo = firstPattern["product"]?.ToString() ?? "";
                            }
                        }
                    }
                }
            }
            
            // Parse the AI recommendations to extract key insights
            if (!string.IsNullOrEmpty(aiRecommendations))
            {
                // Extract failure reason from AI response
                var failureMatch = System.Text.RegularExpressions.Regex.Match(aiRecommendations, @"\*\*Failure Reason\*\*:\s*([^\*\n]+)", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (failureMatch.Success)
                {
                    failureReason = CleanTextExtraction(failureMatch.Groups[1].Value);
                }
                
                // Extract team success pattern
                var teamMatch = System.Text.RegularExpressions.Regex.Match(aiRecommendations, @"\*\*Team Success Pattern\*\*:\s*([^\*\n]+)", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (teamMatch.Success)
                {
                    teamSuccessInfo = CleanTextExtraction(teamMatch.Groups[1].Value);
                }
            }
            
            // Build the human-readable response with actual AI insights
            response += $"❌ **Your order failed because:** {failureReason}\n\n";
            
            if (!string.IsNullOrEmpty(teamSuccessInfo))
            {
                response += $"✅ **Your team member success pattern:** {teamSuccessInfo}\n\n";
            }
            
            // Include AI recommendations if available
            if (!string.IsNullOrEmpty(aiRecommendations))
            {
                var recommendedFixMatch = System.Text.RegularExpressions.Regex.Match(aiRecommendations, @"\*\*Recommended Fix\*\*:\s*(.+?)(?=\*\*Immediate Actions\*\*|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (recommendedFixMatch.Success)
                {
                    var recommendedFix = CleanTextExtraction(recommendedFixMatch.Groups[1].Value);
                    response += $"💡 **Recommended fix:** {recommendedFix}\n\n";
                }
                
                // Extract immediate actions
                var actionsMatch = System.Text.RegularExpressions.Regex.Match(aiRecommendations, @"\*\*Immediate Actions\*\*:\s*(.+?)(?=\*\*|$)", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (actionsMatch.Success)
                {
                    var actions = CleanTextExtraction(actionsMatch.Groups[1].Value);
                    response += $"🎯 **Next steps:** {actions}\n\n";
                }
            }
            
            response += "🤖 **Do you want me to update your order with the correct values and resubmit it?**";
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating analysis response");
            return "I've analyzed your rejected order and found that it needs updates based on your team's successful patterns. Would you like me to help fix your order with the correct values?";
        }
    }

    private string CleanTextExtraction(string text)
    {
        return text?.Trim()
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("  ", " ")
            .Replace("   ", " ")
            .Trim() ?? "";
    }

    private async Task<AgentResponse> HandleGeneralHelp(string userId, string message, ConversationState conversation)
    {
        var helpMessage = @"I'm your AI ordering assistant! I can help you with:

• Get order details - ""Show me details for order TEAM-SUCCESS-001""
• Analyze failed orders - ""Why was order TEAM-FAIL-001 rejected?""
• Update orders with recommendations - ""Fix my rejected order""

Just ask me about any order using its order number or ID!";

        return new AgentResponse
        {
            Message = helpMessage,
            ConversationId = conversation.Id,
            RequiresConfirmation = false
        };
    }
}

/// <summary>
/// Result of AI intent detection
/// </summary>
public class IntentResult
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";
    
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }
}

/// <summary>
/// Agent response model
/// </summary>
public class AgentResponse
{
    public string Message { get; set; } = "";
    public string ConversationId { get; set; } = "";
    public bool RequiresConfirmation { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
