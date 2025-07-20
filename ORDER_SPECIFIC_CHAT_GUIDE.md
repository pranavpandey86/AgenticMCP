# 🎯 Order-Specific Chat Integration Guide

## Overview
Users can now ask about specific failed orders by providing the **Order Number** or **Order ID** in their chat messages. The AI will analyze the specific order and provide targeted recommendations.

## 📋 Available Order IDs for Testing

### **Primary Test Order:**
- **Order Number**: `TEAM-FAIL-001`
- **Full Order ID**: `order_db771936-5ce0-455c-a402-90b56fdcd8cb`
- **User**: David Rodriguez (Marketing)
- **Product**: Adobe Creative Cloud 2023
- **Status**: REJECTED
- **Reason**: Outdated version requested

## 🗣️ Natural Chat Examples

### **Order Number Recognition Patterns:**
```
"Why did my order TEAM-FAIL-001 get rejected?"
"What happened to order TEAM-FAIL-001?"
"Can you check order number TEAM-FAIL-001?"
"I need help with my rejected order TEAM-FAIL-001"
"Order TEAM-FAIL-001 was denied - why?"
```

### **Order ID Recognition Patterns:**
```
"Check order order_db771936-5ce0-455c-a402-90b56fdcd8cb"
"Why was order order_db771936-5ce0-455c-a402-90b56fdcd8cb rejected?"
```

## 🔧 Implementation Guide

### **Step 1: Order Detection in Chat**
```typescript
// Detect order references in user messages
function extractOrderReference(message: string): string | null {
  // Pattern 1: Order Number (TEAM-FAIL-001, ORD-123, etc.)
  const orderNumberPattern = /\b([A-Z]+-[A-Z]+-\d+|[A-Z]+\d+|\d+)\b/g;
  
  // Pattern 2: Full Order ID (order_uuid format)
  const orderIdPattern = /order_[a-f0-9-]{36}/g;
  
  const orderNumberMatch = message.match(orderNumberPattern);
  const orderIdMatch = message.match(orderIdPattern);
  
  return orderIdMatch?.[0] || orderNumberMatch?.[0] || null;
}

// Example usage
const userMessage = "Why did my order TEAM-FAIL-001 get rejected?";
const orderId = extractOrderReference(userMessage);
// Returns: "TEAM-FAIL-001"
```

### **Step 2: Intent Recognition**
```typescript
function isOrderSpecificQuestion(message: string): boolean {
  const orderKeywords = ['order', 'purchase', 'request'];
  const questionKeywords = ['why', 'what', 'help', 'check', 'status'];
  const issueKeywords = ['rejected', 'failed', 'denied', 'problem'];
  
  const hasOrderRef = extractOrderReference(message) !== null;
  const hasOrderKeyword = orderKeywords.some(k => message.toLowerCase().includes(k));
  const hasQuestionKeyword = questionKeywords.some(k => message.toLowerCase().includes(k));
  const hasIssueKeyword = issueKeywords.some(k => message.toLowerCase().includes(k));
  
  return hasOrderRef && hasOrderKeyword && (hasQuestionKeyword || hasIssueKeyword);
}
```

### **Step 3: MCP Tool Integration**
```typescript
async function analyzeSpecificOrder(orderId: string) {
  const analysis = await mcpClient.callTool({
    name: "analyze_order_failures",
    arguments: {
      orderId: orderId,
      analysisType: "all"
    }
  });
  
  return analysis;
}
```

### **Step 4: Response Formatting**
```typescript
function formatOrderSpecificResponse(analysis: any): string {
  const order = analysis.data.analysisContext.targetOrder;
  const rejection = order.approvalWorkflow.history[0];
  const recommendations = analysis.data.recommendations.immediateActions;
  
  return `
📦 **Order ${order.orderNumber} Analysis**

❌ **Why it was rejected:**
${rejection.reason}: ${rejection.comments}

✅ **What your teammates did successfully:**
${recommendations.filter(r => r.includes('Team Success')).join('\n')}

🎯 **How to fix this:**
1. **Update product version**: Request current version instead
2. **Improve justification**: Include specific business impact
3. **Follow successful patterns**: Copy approaches that worked for colleagues

💡 **Ready to reorder correctly?** I can help you create a revised order that addresses these issues.
  `;
}
```

## 🧪 Testing Commands

### **Test with Order Number:**
```bash
curl -X POST "http://localhost:5001/api/mcp/tools/analyze_order_failures/execute" \
  -H "Content-Type: application/json" \
  -d '{"orderId": "TEAM-FAIL-001", "analysisType": "all"}'
```

### **Test with Full Order ID:**
```bash
curl -X POST "http://localhost:5001/api/mcp/tools/analyze_order_failures/execute" \
  -H "Content-Type: application/json" \
  -d '{"orderId": "order_db771936-5ce0-455c-a402-90b56fdcd8cb", "analysisType": "all"}'
```

### **Run Complete Chat Flow Test:**
```bash
./test-order-specific-chat.sh
```

## 🎮 Complete Chat Component Example

```typescript
// chat.component.ts
async handleUserMessage(message: string) {
  const orderId = this.extractOrderReference(message);
  
  if (orderId && this.isOrderSpecificQuestion(message)) {
    this.showTypingIndicator();
    
    try {
      const analysis = await this.mcpService.analyzeOrderFailures(orderId, 'all');
      const response = this.formatOrderSpecificResponse(analysis);
      this.addMessage('AI Assistant', response);
      
      // Offer follow-up actions
      this.suggestFollowUpActions(analysis);
      
    } catch (error) {
      this.addMessage('AI Assistant', 
        `I couldn't find order ${orderId}. Please check the order number and try again.`);
    }
  }
}

private suggestFollowUpActions(analysis: any) {
  const order = analysis.data.analysisContext.targetOrder;
  
  if (order.status === 'rejected') {
    this.addMessage('AI Assistant', 
      `Would you like me to help you create a corrected version of order ${order.orderNumber}?`, 
      ['Yes, help me reorder', 'Show me teammate examples', 'Explain the requirements']
    );
  }
}
```

## 📊 Expected Response Structure

```json
{
  "orderDetails": {
    "orderNumber": "TEAM-FAIL-001",
    "requester": "David Rodriguez",
    "product": "Adobe Creative Cloud 2023",
    "status": "rejected",
    "amount": "$45.99"
  },
  "rejectionReason": {
    "reason": "Outdated version requested",
    "comments": "Adobe Creative Cloud 2023 is discontinued...",
    "rejectedBy": "Jennifer Davis"
  },
  "teamSuccessExamples": [
    "Lisa Johnson successfully ordered Adobe Creative Cloud 2024"
  ],
  "recommendations": [
    "Request Adobe Creative Cloud 2024 instead",
    "Include detailed business justification",
    "Follow successful teammate patterns"
  ]
}
```

## 🎯 Key Benefits

1. **Specific Context**: Users get analysis for their exact order
2. **Detailed History**: Shows approval workflow and rejection reasons  
3. **Team Examples**: Highlights what colleagues did successfully
4. **Actionable Steps**: Provides specific fixes for the failed order
5. **Easy Reference**: Works with both order numbers and full IDs

## 🚀 Quick Start

**Test Order for Chat**: `TEAM-FAIL-001`

**Example User Question**: "Why did my order TEAM-FAIL-001 get rejected?"

**Expected AI Response**: Detailed analysis with specific rejection reason, team success examples, and actionable recommendations to fix and reorder correctly.
