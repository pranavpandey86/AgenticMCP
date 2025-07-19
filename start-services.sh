#!/bin/bash

# Start script for Agentic MCP Server and Chat UI

echo "🚀 Starting Agentic MCP Server and Chat UI..."

# Function to check if a port is in use
check_port() {
    lsof -i :$1 > /dev/null 2>&1
}

# Check if MCP server is running
if check_port 5001; then
    echo "✅ MCP Server is already running on port 5001"
else
    echo "🔧 Starting MCP Server..."
    cd src/AgenticOrderingSystem.API
    dotnet run &
    MCP_PID=$!
    echo "📝 MCP Server PID: $MCP_PID"
    cd ../..
    sleep 5
fi

# Check if Chat UI is running
if check_port 4200; then
    echo "✅ Chat UI is already running on port 4200"
else
    echo "🎨 Starting Chat UI..."
    cd chat-ui
    npm start &
    CHAT_PID=$!
    echo "📝 Chat UI PID: $CHAT_PID"
    cd ..
fi

echo ""
echo "🌟 Both services are starting up!"
echo ""
echo "📊 Services:"
echo "   • MCP Server:  http://localhost:5001"
echo "   • Chat UI:     http://localhost:4200"
echo ""
echo "🔗 API Endpoints:"
echo "   • Health:      http://localhost:5001/api/mcp/health"
echo "   • Tools:       http://localhost:5001/api/mcp/tools"
echo "   • Chat:        http://localhost:5001/api/mcp/ai/conversation"
echo ""
echo "💡 To stop services:"
echo "   • Press Ctrl+C in this terminal"
echo "   • Or run: ./stop-services.sh"
echo ""

# Wait for user interrupt
trap 'echo "🛑 Stopping services..."; kill $MCP_PID $CHAT_PID 2>/dev/null; exit' INT

# Keep script running
while true; do
    sleep 1
done
