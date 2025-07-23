# 🎯 EXACT ORDER TO TEST THE WORKFLOW
# ===================================
# Copy and paste these commands one by one to test the exact workflow

BASE_URL="http://localhost:5001"

# 📋 STEP 1: Setup test data
echo "Setting up test data..."
curl -X POST "$BASE_URL/api/dev/seed-team-orders" \
  -H "Content-Type: application/json" \
  -d '{}'

# 📋 STEP 2: Create and reject an order (optional - can use existing)
echo "Creating test order..."
ORDER_RESPONSE=$(curl -X POST "$BASE_URL/api/orders" \
  -H "Content-Type: application/json" \
  -d '{
    "requesterId": "test_user_manual",
    "productId": "prod_laptop_001", 
    "quantity": 1,
    "businessJustification": "Need laptop",
    "priority": "medium"
  }')
echo $ORDER_RESPONSE

# Get the order ID and reject it
ORDER_ID="paste_order_id_here"
curl -X POST "$BASE_URL/api/orders/$ORDER_ID/reject" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "manager_001",
    "reason": "insufficient_justification", 
    "comments": "Business justification too brief"
  }'

# 🚀 WORKFLOW TESTING COMMANDS
# ============================

# 👤 STEP 3: User asks "Why was my order rejected?"
echo "USER: Why was my order rejected?"
RESPONSE1=$(curl -X POST "$BASE_URL/api/agent/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "test_user_manual",
    "message": "Why was my order rejected?",
    "conversationId": ""
  }')
echo $RESPONSE1 | jq '.'

# Get conversation ID for next step
CONV_ID=$(echo $RESPONSE1 | jq -r '.conversationId')
echo "Conversation ID: $CONV_ID"

# 🤖 STEP 4: Agent analyzes and offers to update (should happen automatically above)
echo "Agent analysis completed. Check if requiresConfirmation: true"

# 👤 STEP 5: User confirms "Yes, please update it"
echo "USER: Yes, please update it"
RESPONSE2=$(curl -X POST "$BASE_URL/api/agent/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "test_user_manual",
    "message": "Yes, please update my order",
    "conversationId": "'$CONV_ID'"
  }')
echo $RESPONSE2 | jq '.'

# 🤖 STEP 6: Agent updates order (should happen automatically above)
echo "Agent should update order and change status to 'created'"

# 🔍 VERIFICATION: Check final order status
echo "Checking final order status..."
curl "$BASE_URL/api/orders/$ORDER_ID" | jq '.status'

# ✅ Expected result: Status should be "created" and order should be updated
