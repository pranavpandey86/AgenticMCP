#!/bin/bash

# Chat Flow Test: User Asks About Failed Order
# Simulates natural conversation where user asks why their order failed

echo "💬 Natural Chat Flow: Why Did My Order Fail?"
echo "============================================="

BASE_URL="http://localhost:5001"

echo ""
echo "👤 USER (David Rodriguez): 'Why did my order get rejected?'"
echo ""
echo "🤖 AI ASSISTANT ANALYSIS:"
echo "------------------------"

# Simulate AI assistant analyzing the user's failed order
echo "🔍 Let me check your order history and team patterns..."
echo ""

# Get the failure analysis for David
ANALYSIS=$(curl -s -X POST "$BASE_URL/api/mcp/tools/analyze_order_failures/execute" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "mkt_david_designer",
    "analysisType": "all"
  }')

echo "📊 ANALYSIS RESULTS:"
echo "-------------------"

# Extract and format the key information
echo "$ANALYSIS" | jq -r '
  .data.analysisContext.scope as $scope |
  .data.failureAnalysis.commonFailureReasons[0] as $failure |
  .data.successAnalysis.bestPractices as $practices |
  .data.insights.keyFindings as $findings |
  
  "👤 User: \($scope)" |
  . + "\n\n❌ WHY YOUR ORDER FAILED:" |
  . + "\n   Reason: \($failure.reason)" |
  . + "\n   Details: \($failure.exampleComments[0])" |
  . + "\n\n✅ WHAT YOUR TEAMMATES DID SUCCESSFULLY:" |
  . + "\n   Based on analysis of your Marketing team:" |
  . + ("\n   " + ($practices | map("• \(.)") | join("\n   "))) |
  . + "\n\n💡 KEY INSIGHTS:" |
  . + ("\n   " + ($findings | map("• \(.)") | join("\n   "))) |
  . + "\n\n🎯 RECOMMENDED NEXT STEPS:" |
  . + "\n   1. Request Adobe Creative Cloud 2024 instead of 2023" |
  . + "\n   2. Include detailed business justification (like your teammate Lisa did)" |
  . + "\n   3. Verify current product versions before ordering" |
  . + "\n   4. Consider reaching out to your manager Jennifer Davis for guidance"
'

echo ""
echo "👤 USER FOLLOW-UP: 'Can you show me what my teammate did differently?'"
echo ""

# Show specific teammate success example
echo "🤖 AI RESPONSE:"
echo "---------------"
echo "Based on your team's order history, here's what Lisa Johnson (your teammate) did successfully:"
echo ""

# Get Lisa's successful order details (simulated based on our team data)
echo "✅ LISA'S SUCCESSFUL ORDER:"
echo "   Product: Adobe Creative Cloud 2024 (current version)"
echo "   Business Justification: 'Need Adobe Creative Cloud for content creation including"
echo "   blog graphics, social media visuals, email campaign designs, and promotional materials."
echo "   Will use Photoshop, Illustrator, and InDesign for marketing campaigns targeting Q4 product launches.'"
echo "   Result: ✅ APPROVED by Jennifer Davis (your manager)"
echo "   Approval Time: 2 hours"
echo ""

echo "🔄 KEY DIFFERENCES:"
echo "   ❌ You requested: Adobe CC 2023 (discontinued)"
echo "   ✅ Lisa requested: Adobe CC 2024 (current version)"
echo "   ❌ Your justification: Basic need statement"
echo "   ✅ Lisa's justification: Detailed use cases with specific projects"
echo ""

echo "👤 USER: 'How do I fix this and reorder correctly?'"
echo ""

echo "🤖 AI ACTIONABLE GUIDANCE:"
echo "-------------------------"
echo "Here's exactly what you should do:"
echo ""
echo "1. 🔄 REORDER WITH CURRENT VERSION:"
echo "   → Request 'Adobe Creative Cloud 2024' instead of 2023"
echo ""
echo "2. ✏️ IMPROVE YOUR BUSINESS JUSTIFICATION:"
echo "   → Follow Lisa's example: mention specific use cases"
echo "   → Include project names and deliverables"
echo "   → Explain impact on Q4 marketing campaigns"
echo ""
echo "3. 👥 GET TEAM SUPPORT:"
echo "   → Ask Lisa for her exact justification template"
echo "   → Get pre-approval from Jennifer Davis (your manager)"
echo ""
echo "4. ⏰ TIMING:"
echo "   → Submit Monday-Wednesday for faster approval"
echo "   → Allow 1-2 business days for processing"
echo ""

echo "✅ CHAT FLOW COMPLETE!"
echo ""
echo "🎯 This demonstrates how a user can naturally ask about their failed order"
echo "   and receive team-based recommendations with specific examples."
