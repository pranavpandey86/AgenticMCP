#!/bin/bash

# Stop script for Agentic MCP Server and Chat UI

echo "🛑 Stopping Agentic MCP Server and Chat UI..."

# Function to stop processes on a specific port
stop_port() {
    local port=$1
    local service_name=$2
    
    PID=$(lsof -ti :$port)
    if [ ! -z "$PID" ]; then
        echo "🔴 Stopping $service_name (PID: $PID)"
        kill $PID
        sleep 2
        # Force kill if still running
        if kill -0 $PID 2>/dev/null; then
            echo "🔫 Force killing $service_name"
            kill -9 $PID
        fi
        echo "✅ $service_name stopped"
    else
        echo "ℹ️  $service_name is not running"
    fi
}

# Stop MCP Server (port 5001)
stop_port 5001 "MCP Server"

# Stop Chat UI (port 4200)
stop_port 4200 "Chat UI"

# Also kill any dotnet run processes
echo "🔍 Checking for dotnet processes..."
DOTNET_PIDS=$(ps aux | grep "dotnet run" | grep -v grep | awk '{print $2}')
if [ ! -z "$DOTNET_PIDS" ]; then
    echo "🔴 Stopping dotnet processes: $DOTNET_PIDS"
    echo $DOTNET_PIDS | xargs kill
fi

# Check for Angular CLI processes
echo "🔍 Checking for Angular processes..."
NG_PIDS=$(ps aux | grep "ng serve\|npm start" | grep -v grep | awk '{print $2}')
if [ ! -z "$NG_PIDS" ]; then
    echo "🔴 Stopping Angular processes: $NG_PIDS"
    echo $NG_PIDS | xargs kill
fi

echo ""
echo "✅ All services stopped!"
echo "💡 You can restart with: ./start-services.sh"
