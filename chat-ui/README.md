# Agentic MCP Chat UI

A beautiful Angular chat interface for interacting with the Agentic Model Context Protocol (MCP) server.

## Features

- 🤖 **Real-time AI Chat**: Direct conversation with the MCP-powered AI assistant
- 🔧 **Tool Awareness**: AI understands and can suggest available MCP tools
- 📱 **Responsive Design**: Works great on desktop and mobile devices
- 🎨 **Modern UI**: Beautiful gradient design with smooth animations
- ⚡ **Real-time Status**: Live connection status and server health monitoring
- 🛠️ **Quick Actions**: Preset suggestions for common tasks

## Setup Instructions

### Prerequisites
- Node.js (v16 or higher)
- Angular CLI (v17 or higher)
- Running Agentic MCP Server (on localhost:5001)

### Installation

1. **Navigate to the chat-ui directory:**
   ```bash
   cd chat-ui
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Start the development server:**
   ```bash
   npm start
   ```

4. **Open your browser:**
   Navigate to `http://localhost:4200`

### Configuration

The chat application is configured to connect to the MCP server at `http://localhost:5001`. If your server is running on a different port, update the `apiUrl` in `src/app/services/chat.service.ts`.

## Usage

1. **Start a conversation** by typing in the chat input
2. **Use quick suggestions** for common tasks like "What tools are available?"
3. **Ask about orders** - the AI can help with order management tasks
4. **Check tool capabilities** - discover what MCP tools are available
5. **Monitor connection status** - see real-time server health in the header

## Available Commands

- `npm start` - Start development server
- `npm run build` - Build for production
- `npm run watch` - Build and watch for changes

## Architecture

- **Angular 17** - Modern Angular framework
- **RxJS** - Reactive programming for HTTP calls
- **TypeScript** - Type-safe development
- **CSS3** - Modern styling with gradients and animations

## API Integration

The chat UI integrates with these MCP server endpoints:

- `/api/mcp/health` - Server health check
- `/api/mcp/ai/conversation` - AI conversation endpoint
- `/api/mcp/tools` - Available tools listing
- `/api/mcp/tools/execute` - Tool execution

## Customization

### Styling
Edit `src/styles.css` for global styles or component-specific CSS files for individual components.

### AI Service
Modify `src/app/services/chat.service.ts` to add new API endpoints or change request/response handling.

### Components
The main chat interface is in `src/app/chat/` - customize the template and component logic as needed.

## Troubleshooting

**Connection Issues:**
- Ensure the MCP server is running on localhost:5001
- Check browser console for CORS errors
- Verify API endpoints are accessible

**Build Issues:**
- Run `npm install` to ensure all dependencies are installed
- Check Angular CLI version compatibility

**Runtime Issues:**
- Check the browser console for JavaScript errors
- Verify the MCP server is responding to health checks

## Future Enhancements

- 🔄 **Auto-retry** connection on failure
- 📊 **Chat history** persistence
- 🎯 **Advanced tool integration** with forms
- 🌙 **Dark mode** support
- 📤 **Export chat** functionality
