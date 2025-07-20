#!/bin/bash

# Order-Specific Chat Flow Test
# User asks about a specific failed order using order number

echo "💬 Order-Specific Chat Flow: Why Did Order TEAM-FAIL-001 Fail?"
echo "=============================================================="

BASE_URL="http://localhost:5001"

echo ""
echo "👤 USER: 'Why did my order TEAM-FAIL-001 get rejected?'"
echo ""
echo "🤖 AI ASSISTANT ANALYSIS:"
echo "------------------------"

# Simulate AI assistant detecting the order number and analyzing it
echo "🔍 I found order TEAM-FAIL-001 in your order history. Let me analyze it..."
echo ""

# Get the failure analysis for the specific order
ANALYSIS=$(curl -s -X POST "$BASE_URL/api/mcp/tools/analyze_order_failures/execute" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "TEAM-FAIL-001",
    "analysisType": "all"
  }')

echo "📋 ORDER DETAILS:"
echo "-----------------"

# Extract and format the order information
echo "$ANALYSIS" | jq -r '
  .data.analysisContext.targetOrder as $order |
  "📦 Order: \($order.orderNumber)" |
  . + "\n👤 Requested by: \($order.requesterInfo.fullName) (\($order.requesterInfo.department))" |
  . + "\n🛍️  Product: \($order.productInfo.name)" |
  . + "\n💰 Amount: $\($order.totalAmount) \($order.currency)" |
  . + "\n📅 Submitted: \($order.createdAt[:10])" |
  . + "\n❌ Status: \($order.status | ascii_upcase)"
'

echo ""
echo "❌ REJECTION REASON:"
echo "-------------------"

# Show the specific rejection reason
echo "$ANALYSIS" | jq -r '
  .data.analysisContext.targetOrder.approvalWorkflow.history[0] as $rejection |
  "👨‍💼 Rejected by: \($rejection.userName)" |
  . + "\n📝 Reason: \($rejection.reason)" |
  . + "\n💬 Comments: \($rejection.comments)"
'

echo ""
echo "✅ TEAM SUCCESS EXAMPLE:"
echo "------------------------"

# Show what teammates did successfully
echo "$ANALYSIS" | jq -r '
  .data.recommendations.immediateActions[] | 
  select(contains("Lisa Johnson")) |
  "🎯 " + .'

echo ""
echo "💡 INTELLIGENT RECOMMENDATIONS:"
echo "-------------------------------"

# Show specific recommendations for this order
echo "$ANALYSIS" | jq -r '
  .data.recommendations.immediateActions[] | 
  select(contains("Address common")) |
  "🔧 " + .'

echo ""
echo "$ANALYSIS" | jq -r '
  .data.recommendations.immediateActions[] | 
  select(contains("Product Update")) |
  "⚠️  " + .'

echo ""
echo "$ANALYSIS" | jq -r '
  .data.recommendations.immediateActions[] | 
  select(contains("Suggestion")) |
  "💡 " + .'

echo ""
echo "🎯 NEXT STEPS FOR ORDER TEAM-FAIL-001:"
echo "======================================="
echo "1. 🔄 REORDER WITH CORRECT VERSION:"
echo "   → Change from: Adobe Creative Cloud 2023"
echo "   → Change to: Adobe Creative Cloud 2024"
echo ""
echo "2. 📝 IMPROVE BUSINESS JUSTIFICATION:"
echo "   → Current: 'Need Adobe Creative Cloud for marketing materials'"
echo "   → Suggested: Include specific projects, deliverables, and impact"
echo ""
echo "3. 👥 FOLLOW TEAMMATE SUCCESS PATTERN:"
echo "   → Lisa Johnson got approval for the same type of software"
echo "   → Copy her approach: detailed justification + current version"
echo ""
echo "4. ⚡ QUICK REORDER:"
echo "   → Use order number reference: TEAM-FAIL-001-REVISED"
echo "   → Submit early in the week for faster processing"
echo ""

echo "👤 USER FOLLOW-UP: 'Can you help me reorder this correctly?'"
echo ""

echo "🤖 AI RESPONSE:"
echo "---------------"
echo "✅ I can help you create a corrected version of order TEAM-FAIL-001!"
echo ""
echo "📋 CORRECTED ORDER DRAFT:"
echo "------------------------"
echo "🛍️  Product: Adobe Creative Cloud 2024 (current version)"
echo "💰 Amount: $52.99 (updated pricing)"
echo "📝 Business Justification:"
echo "   'Need Adobe Creative Cloud 2024 for Q4 marketing campaign materials including:"
echo "   - Blog graphics and social media visuals"
echo "   - Email campaign designs and promotional materials"
echo "   - Product brochures and presentation templates"
echo "   Will use Photoshop, Illustrator, and InDesign for deliverables due by Sept 30th.'"
echo ""
echo "🎯 This follows the successful pattern used by your teammate Lisa Johnson"
echo "   and addresses the version issue that caused the original rejection."
echo ""

echo "✅ ORDER-SPECIFIC CHAT FLOW COMPLETE!"
echo ""
echo "🎯 This demonstrates how users can ask about specific order numbers"
echo "   and get detailed analysis with actionable recommendations."
