#!/bin/bash

# Enhanced Chat Flow: Specific Value Comparisons
# Shows exactly what failed vs what succeeded

echo "💬 Enhanced Chat: Why Did My Order TEAM-FAIL-001 Fail?"
echo "====================================================="

BASE_URL="http://localhost:5001"

echo ""
echo "👤 USER: 'Why did my order TEAM-FAIL-001 get rejected?'"
echo ""

# Get the enhanced analysis with specific comparisons
ANALYSIS=$(curl -s -X POST "$BASE_URL/api/mcp/tools/analyze_order_failures/execute" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "TEAM-FAIL-001",
    "analysisType": "all"
  }')

echo "🤖 AI ASSISTANT:"
echo "---------------"
echo "I analyzed your failed order TEAM-FAIL-001 and compared it with your teammate's successful orders. Here's exactly what went wrong and what worked:"
echo ""

# Show the specific value comparisons
echo "$ANALYSIS" | jq -r '
  .data.recommendations.immediateActions[] | 
  select(contains("TEAM SUCCESS COMPARISON") or 
         contains("PRODUCT VERSION") or 
         contains("BUSINESS JUSTIFICATION") or 
         contains("AMOUNT") or
         contains("❌") or 
         contains("✅")) |
  .
'

echo ""
echo "💡 SUMMARY:"
echo "----------"
echo "Your order failed because:"
echo "1. 🚫 You requested an OUTDATED version (Adobe CC 2023)"
echo "2. 📝 Your justification was TOO SHORT (49 characters)"
echo "3. 💰 Your pricing was slightly OUTDATED ($45.99 vs $52.99)"
echo ""
echo "Lisa Johnson succeeded because:"
echo "1. ✅ She requested the CURRENT version (Adobe CC 2024)"
echo "2. 📝 She provided a DETAILED justification (245 characters)"
echo "3. 💰 She used CURRENT pricing ($52.99)"
echo ""

echo "👤 USER FOLLOW-UP: 'So what exactly should I change to get approval?'"
echo ""

echo "🤖 AI RESPONSE:"
echo "---------------"
echo "Based on Lisa's successful pattern, here are the EXACT changes you need:"
echo ""
echo "🔄 PRODUCT VERSION:"
echo "   Change: Adobe Creative Cloud 2023 → Adobe Creative Cloud 2024"
echo ""
echo "📝 BUSINESS JUSTIFICATION:"
echo "   Change your 49-character justification to something like Lisa's 245-character version:"
echo "   "
echo "   FROM: 'Need Adobe Creative Cloud for marketing materials'"
echo "   "
echo "   TO: 'Need Adobe Creative Cloud 2024 for content creation including:"
echo "   - Blog graphics and social media visuals"
echo "   - Email campaign designs and promotional materials"
echo "   - Product brochures for Q4 marketing campaigns"
echo "   Will use Photoshop, Illustrator, and InDesign for deliverables due by Sept 30th."
echo "   This software is essential for maintaining brand consistency across all marketing channels.'"
echo ""
echo "💰 PRICING:"
echo "   Update: $45.99 → $52.99 (current Adobe pricing)"
echo ""
echo "🎯 RESULT: Following this exact pattern gives you the same success rate as Lisa!"
echo ""

echo "✅ ENHANCED CHAT FLOW COMPLETE!"
echo ""
echo "🎯 Now the AI shows SPECIFIC VALUES from successful teammates"
echo "   instead of generic best practices!"
