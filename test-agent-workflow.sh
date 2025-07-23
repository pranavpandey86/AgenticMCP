#!/bin/bash

# Agent workflow test script
echo "🤖 Testing Agent Workflow Integration"
echo "======================================"

# Set base URL
BASE_URL="http://localhost:5000"

# Function to make HTTP requests with pretty output
make_request() {
    local method=$1
    local endpoint=$2
    local data=$3
    local description=$4
    
    echo ""
    echo "📝 $description"
    echo "➡️  $method $endpoint"
    
    if [ -n "$data" ]; then
        echo "📦 Request Body:"
        echo "$data" | jq .
        response=$(curl -s -X $method "$BASE_URL$endpoint" \
                      -H "Content-Type: application/json" \
                      -d "$data")
    else
        response=$(curl -s -X $method "$BASE_URL$endpoint")
    fi
    
    echo "📨 Response:"
    echo "$response" | jq .
    echo "----------------------------------------"
    
    # Return response for potential reuse
    echo "$response"
}

# Test 1: Start a chat asking about rejected order
echo "🚀 Test 1: User asks why their order was rejected"
CHAT_RESPONSE=$(make_request "POST" "/api/agent/chat" '{
    "message": "Why was my order rejected?",
    "userId": "demo-user-001"
}')

# Extract conversation ID
CONVERSATION_ID=$(echo "$CHAT_RESPONSE" | jq -r '.conversationId // empty')

if [ -z "$CONVERSATION_ID" ]; then
    echo "❌ Failed to get conversation ID"
    exit 1
fi

echo "✅ Conversation ID: $CONVERSATION_ID"

# Test 2: Check if the response requires confirmation
REQUIRES_CONFIRMATION=$(echo "$CHAT_RESPONSE" | jq -r '.requiresConfirmation // false')

if [ "$REQUIRES_CONFIRMATION" = "true" ]; then
    echo ""
    echo "🚀 Test 2: User confirms order update"
    make_request "POST" "/api/agent/confirm" "{
        \"conversationId\": \"$CONVERSATION_ID\",
        \"confirmed\": true,
        \"userId\": \"demo-user-001\"
    }" "User confirms the order update"
else
    echo ""
    echo "ℹ️  Test 2: No confirmation required (analysis completed without actionable suggestions)"
fi

# Test 3: Start a new conversation with general help
echo ""
echo "🚀 Test 3: User asks for general help"
make_request "POST" "/api/agent/chat" '{
    "message": "What can you help me with?",
    "userId": "demo-user-001"
}' "User asks for general help"

# Test 4: Test with specific order ID (if you have one)
echo ""
echo "🚀 Test 4: User asks about specific order"
make_request "POST" "/api/agent/chat" '{
    "message": "Why was order 67890abcdef1234567890123 rejected?",
    "userId": "demo-user-001"
}' "User asks about specific order ID"

# Test 5: Test declining confirmation
echo ""
echo "🚀 Test 5: User declines order update"
NEW_CHAT_RESPONSE=$(make_request "POST" "/api/agent/chat" '{
    "message": "Why was my order rejected?",
    "userId": "demo-user-002"
}')

NEW_CONVERSATION_ID=$(echo "$NEW_CHAT_RESPONSE" | jq -r '.conversationId // empty')

if [ -n "$NEW_CONVERSATION_ID" ]; then
    NEW_REQUIRES_CONFIRMATION=$(echo "$NEW_CHAT_RESPONSE" | jq -r '.requiresConfirmation // false')
    
    if [ "$NEW_REQUIRES_CONFIRMATION" = "true" ]; then
        make_request "POST" "/api/agent/confirm" "{
            \"conversationId\": \"$NEW_CONVERSATION_ID\",
            \"confirmed\": false,
            \"userId\": \"demo-user-002\"
        }" "User declines the order update"
    fi
fi

echo ""
echo "🎉 Agent workflow tests completed!"
echo "=================================="
echo ""
echo "📊 Summary:"
echo "✅ Chat endpoint tested"
echo "✅ Confirmation endpoint tested"
echo "✅ Order analysis workflow tested"
echo "✅ Update workflow tested"
echo ""
echo "💡 Next steps:"
echo "1. Check the responses to verify agent behavior"
echo "2. Test with real order data"
echo "3. Integrate with chat UI"
echo "4. Add more sophisticated AI decision-making"
