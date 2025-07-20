# 🔧 Troubleshooting Guide: Order Failure Analysis

*Solutions for common issues and best practices*

---

## 🚨 Common Issues & Solutions

### **Issue 1: "No failure patterns found"**

**Symptoms:**
- Analysis returns empty results
- "Insufficient data" messages
- Confidence score shows 0%

**Causes & Solutions:**

#### **Insufficient Historical Data**
```
Problem: Less than 10 orders in the system
Solution: 
1. Run data seeding: ./test-mock-data.sh
2. Wait for more orders to accumulate
3. Adjust time range to longer period
```

#### **All Orders Are Successful**
```
Problem: No rejected orders in the dataset
Solution:
1. Create test rejections with different reasons
2. Use broader time range (quarter/year instead of week)
3. Check if rejection reasons are being recorded properly
```

#### **Wrong Analysis Scope**
```
Problem: Analyzing specific user/product with no failures
Solution:
1. Use system-wide analysis: "Analyze failure patterns overall"
2. Remove specific filters: orderId, userId, productId
3. Try different analysis types: 'all' instead of 'failure_reasons'
```

---

### **Issue 2: "Analysis takes too long"**

**Symptoms:**
- Requests timeout after 30+ seconds
- Server becomes unresponsive
- High memory usage

**Causes & Solutions:**

#### **Large Dataset Processing**
```
Problem: Analyzing thousands of orders
Solution:
1. Reduce time range: use 'month' instead of 'year'
2. Add specific filters: userId, productId, or category
3. Use pagination for large results
```

#### **Complex Analysis Type**
```
Problem: 'all' analysis type is resource-intensive
Solution:
1. Use specific analysis: 'failure_reasons' or 'success_patterns'
2. Run analyses separately instead of combined
3. Increase server memory allocation
```

#### **Database Performance**
```
Problem: MongoDB queries are slow
Solution:
1. Check MongoDB Atlas connection
2. Add indexes on frequently queried fields
3. Consider database optimization
```

---

### **Issue 3: "Recommendations are not relevant"**

**Symptoms:**
- Generic or unhelpful suggestions
- Low confidence scores
- Recommendations don't match the context

**Causes & Solutions:**

#### **Poor Data Quality**
```
Problem: Rejection reasons are too vague
Solution:
1. Ensure detailed rejection reasons in orders
2. Update order creation to capture more context
3. Train users to provide specific failure information
```

#### **Limited Context**
```
Problem: Analysis lacks sufficient similar orders
Solution:
1. Use broader analysis scope (remove specific filters)
2. Analyze at category level instead of specific products
3. Include longer time ranges for more data
```

#### **Algorithm Tuning Needed**
```
Problem: Recommendation logic needs improvement
Solution:
1. Update AnalyzeOrderFailuresTool.cs algorithms
2. Adjust confidence scoring thresholds
3. Enhance pattern matching logic
```

---

### **Issue 4: "Server errors during analysis"**

**Symptoms:**
- 500 Internal Server Error
- "Tool execution failed" messages
- Exception logs in server output

**Causes & Solutions:**

#### **Database Connection Issues**
```
Problem: MongoDB Atlas connection failed
Solution:
1. Check internet connectivity
2. Verify MongoDB Atlas credentials in appsettings.json
3. Restart the server: dotnet run
```

#### **Memory Issues**
```
Problem: OutOfMemoryException during large analyses
Solution:
1. Reduce analysis scope with filters
2. Increase server memory allocation
3. Implement result pagination
```

#### **Code Exceptions**
```
Problem: Bugs in analysis logic
Solution:
1. Check server logs for specific errors
2. Test with simple queries first
3. Update AnalyzeOrderFailuresTool.cs if needed
```

---

### **Issue 5: "UI doesn't format results properly"**

**Symptoms:**
- Raw JSON displayed instead of formatted text
- Missing formatting for lists/sections
- Analysis results appear garbled

**Causes & Solutions:**

#### **Frontend Formatting Issues**
```
Problem: Angular component doesn't recognize analysis results
Solution:
1. Check chat.component.ts formatSuccessfulToolResult method
2. Ensure tool name matches 'analyze_order_failures'
3. Update UI formatting logic if needed
```

#### **Data Structure Changes**
```
Problem: Tool output format changed but UI wasn't updated
Solution:
1. Update Angular formatting to match new structure
2. Check tool output structure in developer tools
3. Test with simple analysis first
```

---

## 🧪 Testing & Debugging

### **Step 1: Basic Functionality Test**
```bash
# Start the server
dotnet run --project src/AgenticOrderingSystem.API/

# In another terminal, test basic endpoint
curl -X POST http://localhost:5076/api/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Analyze failure patterns"}'
```

### **Step 2: Seed Test Data**
```bash
# Ensure you have test data
./test-mock-data.sh

# Run the demo script
./demo-failure-analysis.sh
```

### **Step 3: Check Tool Registration**
```bash
# Verify tool is registered
curl http://localhost:5076/api/mcp/tools

# Should show "analyze_order_failures" in the list
```

### **Step 4: Debug with Specific Query**
```bash
# Test with specific order
curl -X POST http://localhost:5076/api/mcp/chat \
  -H "Content-Type: application/json" \
  -d '{"message": "Why was order REJ-2025-07-0001 rejected?"}'
```

### **Step 5: UI Testing**
```bash
# Start the Angular frontend
cd chat-ui
npm start

# Navigate to http://localhost:4200
# Test with: "Analyze failure patterns for recent orders"
```

---

## 📋 Best Practices

### **For Users**

#### **Effective Query Patterns**
```
✅ Good: "Analyze failure patterns for cybersecurity orders from last month"
❌ Avoid: "Why do my orders fail?"

✅ Good: "What are success factors for orders above $10,000?"
❌ Avoid: "How to get orders approved?"

✅ Good: "Show me rejection reasons for user_emp_jane's orders"
❌ Avoid: "Jane's order problems"
```

#### **Understanding Results**
- **Confidence Score**: >70% = reliable, <50% = need more data
- **Percentages**: Based on analyzed orders, not universal truths
- **Recommendations**: Prioritize "High impact" suggestions first

### **For Developers**

#### **Adding New Analysis Types**
1. Update `AnalysisType` enum in ToolModels.cs
2. Add corresponding logic in AnalyzeOrderFailuresTool.cs
3. Update UI formatting in chat.component.ts
4. Add tests for new functionality

#### **Performance Optimization**
```csharp
// Use filtering early in the pipeline
var filteredOrders = orders
    .Where(o => o.CreatedAt >= startDate)
    .Where(o => o.Status == targetStatus)
    .ToList(); // Execute query once

// Avoid multiple database calls in loops
```

#### **Error Handling**
```csharp
try
{
    var result = await AnalyzeFailurePatterns(orders);
    return CreateSuccessResponse(result);
}
catch (Exception ex)
{
    return CreateErrorResponse($"Analysis failed: {ex.Message}");
}
```

---

## 🔍 Debugging Checklist

### **Before Reporting Issues**

- [ ] Server is running and accessible
- [ ] Database connection is working
- [ ] Test data exists in the system
- [ ] Tool is registered in MCP system
- [ ] Query syntax is correct
- [ ] Sufficient data exists for analysis
- [ ] No network connectivity issues
- [ ] Frontend is updated and running
- [ ] Browser cache is cleared
- [ ] Recent changes are deployed

### **Information to Include in Bug Reports**

1. **Exact error message** or unexpected behavior
2. **Steps to reproduce** the issue
3. **Query used** that caused the problem
4. **Server logs** around the time of the issue
5. **Environment details** (OS, browser, .NET version)
6. **Dataset size** and characteristics
7. **Expected vs actual results**

---

## 📞 Getting Help

### **Self-Service Resources**
- **README.md**: Basic setup and usage
- **ORDER_FAILURE_ANALYSIS.md**: Feature documentation
- **API Documentation**: `/swagger` endpoint when server is running
- **Demo Scripts**: `./demo-failure-analysis.sh` for testing

### **Community Support**
- **GitHub Issues**: Report bugs and request features
- **Documentation**: Contribute improvements and examples
- **Code Reviews**: Suggest algorithm improvements

### **Emergency Contacts**
For critical production issues:
1. Check server logs immediately
2. Restart services: `./stop-services.sh && ./start-services.sh`
3. Verify database connectivity
4. Roll back recent changes if necessary

---

**Remember: Most issues can be resolved by checking data availability and using appropriate query scopes!** 🎯

*Last updated: July 20, 2025*
