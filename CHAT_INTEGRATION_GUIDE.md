# 💬 Chat Integration Guide: Order Failure Analysis

## Overview
When a user asks about their failed order in natural language, the AI should use the `analyze_order_failures` MCP tool to provide intelligent team-based recommendations.

## 🎯 User Intent Recognition

### Common User Questions:
- "Why did my order get rejected?"
- "What went wrong with my purchase request?"
- "Can you help me understand why my order failed?"
- "Why was my order denied?"
- "What can I do to fix my rejected order?"

### AI Recognition Pattern:
```typescript
// Example intent detection
if (userMessage.includes(['order', 'rejected']) || 
    userMessage.includes(['order', 'failed']) ||
    userMessage.includes(['order', 'denied'])) {
  // Trigger order failure analysis
  analyzeOrderFailure(userId);
}
```

## 🔧 Integration Steps

### Step 1: User Authentication
```javascript
// Get current user context
const userId = getCurrentUser().id; // e.g., "mkt_david_designer"
```

### Step 2: Call MCP Tool
```javascript
const analysis = await mcpClient.callTool({
  name: "analyze_order_failures",
  arguments: {
    userId: userId,
    analysisType: "all"
  }
});
```

### Step 3: Format Response
```javascript
function formatFailureResponse(analysis) {
  const data = analysis.data;
  
  return `
❌ **Why Your Order Failed:**
${data.failureAnalysis.commonFailureReasons[0].reason}
${data.failureAnalysis.commonFailureReasons[0].exampleComments[0]}

✅ **What Your Teammates Did Successfully:**
${data.successAnalysis.bestPractices.map(practice => `• ${practice}`).join('\n')}

💡 **Key Insights:**
${data.insights.keyFindings.map(finding => `• ${finding}`).join('\n')}

🎯 **Recommended Next Steps:**
1. Address the specific failure reason above
2. Follow your teammates' successful patterns
3. Contact your manager for guidance if needed
  `;
}
```

## 🎮 Complete Chat Flow Example

### Angular Chat Component Integration
```typescript
// chat.component.ts
async handleUserMessage(message: string) {
  if (this.isOrderFailureQuestion(message)) {
    this.showTypingIndicator();
    
    try {
      const analysis = await this.mcpService.analyzeOrderFailures(
        this.currentUser.id, 
        'all'
      );
      
      const response = this.formatOrderFailureResponse(analysis);
      this.addMessage('AI Assistant', response);
      
    } catch (error) {
      this.addMessage('AI Assistant', 
        'I had trouble analyzing your order. Please try again or contact support.');
    }
  }
}

private isOrderFailureQuestion(message: string): boolean {
  const orderKeywords = ['order', 'purchase', 'request'];
  const failureKeywords = ['failed', 'rejected', 'denied', 'wrong'];
  
  return orderKeywords.some(ok => message.toLowerCase().includes(ok)) &&
         failureKeywords.some(fk => message.toLowerCase().includes(fk));
}
```

## 🧪 Testing Your Integration

### Test Cases:
1. **Basic Failure Question**: "Why did my order fail?"
2. **Specific Product**: "Why was my Adobe order rejected?"
3. **Follow-up**: "What did my teammates do differently?"
4. **Action Request**: "How can I fix this?"

### Expected Response Format:
```
🤖 I found that your order was rejected because [specific reason].

Looking at your team's successful orders, here's what worked:
• [Best practice 1]
• [Best practice 2]
• [Best practice 3]

Based on this analysis, I recommend:
1. [Specific action]
2. [Team-based suggestion]
3. [Process improvement]

Would you like me to help you resubmit the order with these improvements?
```

## 🔗 API Endpoint
```
POST /api/mcp/tools/analyze_order_failures/execute
Content-Type: application/json

{
  "userId": "user_id_here",
  "analysisType": "all"
}
```

## 💡 Pro Tips

1. **User Context**: Always pass the authenticated user's ID
2. **Natural Language**: Present technical analysis in conversational tone
3. **Actionable**: Provide specific next steps, not just analysis
4. **Team-Based**: Highlight successful teammate examples
5. **Follow-up**: Offer to help with reordering or contacting managers

## 🎯 Success Metrics
- User gets specific failure reason
- User sees successful teammate patterns  
- User receives actionable recommendations
- User can immediately apply suggestions
