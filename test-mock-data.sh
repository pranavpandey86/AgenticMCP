#!/bin/bash

# Test Script for Agentic Ordering System
echo "🚀 Testing Agentic Ordering System Mock Data"
echo "=============================================="

# Navigate to project directory
cd /Users/pranavpandey/AgenticMCP/src/AgenticOrderingSystem.API

echo "📁 Current directory: $(pwd)"

# Check if .env file exists
if [ -f "../../.env" ]; then
    echo "✅ Environment file found"
else
    echo "❌ Environment file not found"
fi

# Build the project
echo "🔨 Building project..."
dotnet build

if [ $? -eq 0 ]; then
    echo "✅ Build successful"
    
    # Run the application
    echo "🚀 Starting application..."
    echo "Application will be available at: http://localhost:5001"
    echo "Test endpoints:"
    echo "- Health Check: http://localhost:5001/api/dev/health"
    echo "- Seed Database: http://localhost:5001/api/dev/seed"
    echo "- Data Summary: http://localhost:5001/api/dev/data-summary"
    echo ""
    echo "Press Ctrl+C to stop the application"
    
    dotnet run
else
    echo "❌ Build failed"
    exit 1
fi
