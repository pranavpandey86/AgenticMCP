#!/bin/bash

echo "🚀 Testing MCP Server Endpoints"
echo "================================"

BASE_URL="http://localhost:5001"

echo "1. Testing MCP Health endpoint..."
curl -X GET "$BASE_URL/api/mcp/health" -H "Content-Type: application/json" -w "\n\nStatus: %{http_code}\n\n"

echo "2. Testing MCP Tools endpoint..."
curl -X GET "$BASE_URL/api/mcp/tools" -H "Content-Type: application/json" -w "\n\nStatus: %{http_code}\n\n"

echo "3. Testing a specific tool execution (get_user_orders)..."
curl -X POST "$BASE_URL/api/mcp/tools/get_user_orders/execute" \
  -H "Content-Type: application/json" \
  -d '{"userId": "user_001"}' \
  -w "\n\nStatus: %{http_code}\n\n"

echo "4. Testing AI chat endpoint..."
curl -X POST "$BASE_URL/api/mcp/ai/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "sessionId": "test_session_001",
    "message": "Hello, can you help me check my order status?",
    "context": {
      "userId": "user_001"
    }
  }' \
  -w "\n\nStatus: %{http_code}\n\n"

echo "✅ MCP Server Testing Complete!"
