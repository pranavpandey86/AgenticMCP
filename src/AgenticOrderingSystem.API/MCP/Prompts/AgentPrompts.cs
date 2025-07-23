namespace AgenticOrderingSystem.API.MCP.Prompts;

/// <summary>
/// Predefined prompts for agent interactions
/// </summary>
public static class AgentPrompts
{
    public const string SYSTEM_PROMPT = @"
You are an intelligent order management assistant. Your role is to help users understand order issues and take corrective actions.

Available Tools:
1. AnalyzeOrderFailures - Analyze why an order was rejected and provide suggestions
2. UpdateOrder - Update an order with new values and change its status

When a user asks about a rejected order:
1. First analyze the order failure using the AnalyzeOrderFailures tool
2. Present the analysis and suggested improvements
3. Ask if they want to update the order with the suggested values
4. If they confirm, use the UpdateOrder tool to apply changes and resubmit

Always be helpful, clear, and guide users through the process step by step.";

    public const string ORDER_REJECTION_ANALYSIS = @"
I'll analyze why your order was rejected and compare it with successful orders from your team members.

Let me check the details...";

    public const string UPDATE_CONFIRMATION_PROMPT = @"
Based on the analysis, I found some issues with your order and have suggestions to fix them:

{analysis_results}

Would you like me to update your order with these recommended values and resubmit it for approval? 

Please respond with 'yes' to proceed or 'no' if you'd like to make different changes.";

    public const string UPDATE_SUCCESS_MESSAGE = @"
✅ Great! I've successfully updated your order with the recommended values:

{updated_values}

Your order status has been changed from 'rejected' to 'created' and is now ready for resubmission to the approval workflow.

The order will go through the standard approval process with the updated information.";

    public const string UPDATE_CANCELLED_MESSAGE = @"
No problem! Your order remains unchanged. 

If you'd like to make different modifications or need help understanding the rejection reasons, feel free to ask!";

    public const string ERROR_MESSAGE = @"
I encountered an issue while processing your request: {error}

Please try again or contact support if the problem persists.";

    public const string GENERAL_HELP = @"
I can help you with:
- Understanding why your order was rejected
- Comparing your order with successful team orders
- Updating and resubmitting orders with recommended changes
- General order management questions

What would you like assistance with?";

    /// <summary>
    /// Generate a prompt for deciding the next action based on user input
    /// </summary>
    public static string GetActionDecisionPrompt(string userMessage, string conversationHistory)
    {
        return $@"
Based on the user's message and conversation history, determine the appropriate next action.

User Message: ""{userMessage}""

Conversation History:
{conversationHistory}

Respond with one of these actions:
- analyze_order_failure: If user asks about rejected orders or wants to understand rejection reasons
- update_order: If user confirms they want to update an order with suggested values
- general_help: For general questions or unclear requests
- complete: If the conversation is finished

Format your response as: ACTION: [action_name]
If additional parameters are needed, include them as: PARAMS: {{key: value, ...}}";
    }

    /// <summary>
    /// Generate response based on tool results
    /// </summary>
    public static string GetResponseGenerationPrompt(string toolOutput, string userMessage)
    {
        return $@"
Based on the tool execution results, generate a helpful response to the user.

User's Original Message: ""{userMessage}""

Tool Output:
{toolOutput}

Generate a clear, helpful response that:
1. Summarizes the results in user-friendly language
2. Provides actionable next steps if applicable
3. Is conversational and supportive in tone";
    }
}
