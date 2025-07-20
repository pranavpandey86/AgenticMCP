#!/bin/bash

# 🎯 Order Failure Analysis Demo Script
# Demonstrates the new intelligent failure analysis feature

echo "🚀 Agentic MCP - Order Failure Analysis Demo"
echo "============================================="
echo

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to make API calls with better formatting
test_analysis() {
    local query="$1"
    local description="$2"
    
    echo -e "${BLUE}🔍 Testing: ${description}${NC}"
    echo -e "${YELLOW}Query: ${query}${NC}"
    echo

    response=$(curl -s -X POST "http://localhost:5001/api/mcp/ai/chat" \
        -H "Content-Type: application/json" \
        -d "{\"message\": \"$query\"}")
    
    if echo "$response" | jq -e '.success' > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Success!${NC}"
        echo "$response" | jq -r '.response' 2>/dev/null || echo "Response received but formatting failed"
    else
        echo -e "${RED}❌ Failed${NC}"
        echo "$response" | jq -r '.error.message' 2>/dev/null || echo "No error message available"
    fi
    
    echo
    echo "---"
    echo
}

# Check if server is running
echo "🔧 Checking server status..."
if ! curl -s "http://localhost:5001/api/mcp/health" > /dev/null; then
    echo -e "${RED}❌ Server not running! Please start with: ./start-services.sh${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Server is running!${NC}"
echo

# Test the new failure analysis tool
echo "🎯 Testing Order Failure Analysis Features"
echo "=========================================="
echo

# Test 1: General failure analysis
test_analysis \
    "Analyze failure patterns for recent orders" \
    "General System-Wide Failure Analysis"

# Test 2: User-specific analysis
test_analysis \
    "Why do orders from user_emp_john get rejected?" \
    "User-Specific Failure Analysis"

# Test 3: Product-specific analysis
test_analysis \
    "What are common rejection reasons for cybersecurity products?" \
    "Product Category Analysis"

# Test 4: Specific order analysis
test_analysis \
    "Analyze similar orders to REJ-2025-07-0001 and suggest improvements" \
    "Order-Specific Analysis with Recommendations"

# Test 5: Success pattern analysis
test_analysis \
    "What makes orders successful? Show me best practices" \
    "Success Pattern Analysis"

echo "🎉 Demo Complete!"
echo
echo "🔍 What this feature provides:"
echo "  • Historical failure pattern analysis"
echo "  • Success factor identification"
echo "  • Intelligent recommendations"
echo "  • Risk mitigation strategies"
echo "  • Confidence scoring"
echo "  • Actionable insights"
echo
echo "🌐 Try it in the web interface: http://localhost:4200"
echo
echo "💡 Example queries to try:"
echo "  - 'Why was order REJ-2025-07-0001 rejected?'"
echo "  - 'Analyze failure patterns for recent orders'"
echo "  - 'What are common rejection reasons?'"
echo "  - 'Show me success factors for approved orders'"
echo "  - 'How can I improve my order approval rate?'"
