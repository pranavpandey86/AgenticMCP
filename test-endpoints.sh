#!/bin/bash

echo "🧪 Testing API Endpoints"
echo "========================"

BASE_URL="http://localhost:5001"

echo ""
echo "1️⃣ Testing Health Check..."
curl -s "$BASE_URL/api/dev/health" | python3 -m json.tool
echo ""

echo "2️⃣ Seeding Database..."
curl -s -X POST "$BASE_URL/api/dev/seed" | python3 -m json.tool
echo ""

echo "3️⃣ Getting Data Summary..."
curl -s "$BASE_URL/api/dev/data-summary" | python3 -m json.tool
echo ""

echo "✅ Testing complete!"
echo ""
echo "📊 Expected Results:"
echo "  - Health: databaseConnected: true"
echo "  - Seed: categoriesCount: 5, productsCount: 6, usersCount: 12"
echo "  - Summary: Detailed breakdown of all data"
