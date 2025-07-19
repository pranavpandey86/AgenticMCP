#!/bin/bash

echo "🚀 Starting Agentic AI Ordering System Test"
echo "=========================================="

# Navigate to project directory
cd "$(dirname "$0")/src/AgenticOrderingSystem.API"

echo "📍 Current directory: $(pwd)"
echo ""

# Check .NET version
echo "🔧 Checking .NET version..."
dotnet --version
echo ""

# Build the project
echo "🔨 Building the project..."
dotnet build --verbosity quiet
if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
else
    echo "❌ Build failed!"
    exit 1
fi
echo ""

# Start the application
echo "🌟 Starting the API server..."
echo "📡 Server will be available at: http://localhost:5001"
echo "🔗 API Documentation: http://localhost:5001/swagger"
echo ""
echo "🧪 Test endpoints after server starts:"
echo "  Health Check: curl http://localhost:5001/api/dev/health"
echo "  Seed Data:    curl -X POST http://localhost:5001/api/dev/seed"
echo "  Data Summary: curl http://localhost:5001/api/dev/data-summary"
echo ""
echo "Press Ctrl+C to stop the server"
echo "=========================================="

# Run the application
dotnet run
