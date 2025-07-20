#!/bin/bash

# Test Team-Based Success Patterns for Failed Orders
# This script tests the enhanced failure analysis with team recommendations

echo "🎯 Testing Team-Based Success Patterns for Failed Orders"
echo "========================================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

BASE_URL="http://localhost:5001"

echo ""
echo -e "${BLUE}Step 1: Verify Team Structure${NC}"
echo "------------------------------"
curl -s "$BASE_URL/api/dev/data-summary" | jq -r '
  .userHierarchy.manager as $mgr |
  "Manager: \($mgr.name) (\($mgr.department))" |
  . + "\nTeam Members:" |
  . + ($mgr.directReports | map("  - \(.name) (\(.role))") | join("\n"))
'

echo ""
echo -e "${YELLOW}Step 2: Test Failed Order Analysis${NC}"
echo "-----------------------------------"
echo "Analyzing David Rodriguez's failed order..."
curl -s -X POST "$BASE_URL/api/mcp/tools/analyze_order_failures/execute" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "mkt_david_designer",
    "analysisType": "failure_reasons"
  }' | jq -r '.data.failureAnalysis.commonFailureReasons[0] | 
  "❌ Failure: \(.reason) (\(.percentage)% of rejections)" |
  . + "\n💬 Comment: \(.exampleComments[0])"'

echo ""
echo -e "${GREEN}Step 3: Get Team Success Recommendations${NC}"
echo "--------------------------------------------"
echo "Getting success patterns from David's teammates..."
curl -s -X POST "$BASE_URL/api/mcp/tools/analyze_order_failures/execute" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "mkt_david_designer",
    "analysisType": "success_patterns"
  }' | jq -r '.data.successAnalysis.bestPractices[] | "✅ \(.)"'

echo ""
echo -e "${BLUE}Step 4: Get Version-Specific Recommendations${NC}"
echo "----------------------------------------------"
echo "Getting specific recommendations for version conflicts..."
curl -s -X POST "$BASE_URL/api/mcp/tools/analyze_order_failures/execute" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "mkt_david_designer",
    "analysisType": "recommendations"
  }' | jq -r '.data.recommendations.processImprovements[] | "🔧 \(.)"'

echo ""
echo -e "${GREEN}Step 5: Complete Team Analysis Summary${NC}"
echo "-------------------------------------------"
echo "Key insights from team-based analysis..."
curl -s -X POST "$BASE_URL/api/mcp/tools/analyze_order_failures/execute" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "mkt_david_designer",
    "analysisType": "all"
  }' | jq -r '.data.insights.keyFindings[] | "💡 \(.)"'

echo ""
echo -e "${YELLOW}Step 6: Test Different Team Member${NC}"
echo "-------------------------------------"
echo "Testing analysis for Lisa Johnson (successful team member)..."
curl -s -X POST "$BASE_URL/api/mcp/tools/analyze_order_failures/execute" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "mkt_lisa_content",
    "analysisType": "all"
  }' | jq -r '.data.analysisContext.scope'

echo ""
echo -e "${GREEN}✅ Team-Based Success Pattern Testing Complete!${NC}"
echo ""
echo "🎯 Expected Results:"
echo "   - David's failed order shows version conflict (Adobe CC 2023 discontinued)"
echo "   - Tool recommends current version (2024.1.0)"
echo "   - Success patterns show teammate Lisa's successful order approach"
echo "   - Provides specific business justification recommendations"
echo "   - Shows team hierarchy with Jennifer Davis as manager"
