# 🤖 Agentic MCP Ordering System

A complete **AI-enhanced Model Context Protocol (MCP) ordering system** that demonstrates natural language interaction with enterprise order management through intelligent tool execution.

## 🌟 Overview

This system showcases how AI can seamlessly interact with complex business workflows using the Model Context Protocol. Users can ask natural language questions like *"Why was order REJ-2025-07-0001 rejected?"* and receive intelligent, human-readable responses with complete workflow analysis.

## ✨ Key Features

### 🚀 Backend (ASP.NET Core 8)
- **MCP Server Integration**: Complete Model Context Protocol implementation
- **Perplexity AI Integration**: Natural language processing with the "sonar" model
- **Advanced Order Management**: Comprehensive approval workflows and business logic
- **MongoDB Integration**: Scalable document database with MongoDB Atlas support
- **Smart Tool Execution**: AI-powered parameter extraction and validation
- **Dual ID Support**: Works with both order numbers (REJ-2025-07-0001) and GUIDs

### 🎨 Frontend (Angular 17)
- **Conversational AI Interface**: Real-time chat with intelligent responses
- **Rich Result Formatting**: Human-readable displays instead of raw JSON
- **Tool Execution Visualization**: Clear feedback on AI tool operations
- **Responsive Design**: Modern UI with status indicators and workflow visualization
- **Real-time Health Monitoring**: Server status and tool availability tracking

### 🧠 AI Capabilities
- **Natural Language Queries**: Convert human questions to database operations
- **Context-Aware Responses**: Intelligent analysis of order statuses and workflows
- **Smart Recommendations**: AI-generated next steps and insights
- **Error Handling**: Graceful fallback and user-friendly error messages

## 🛠️ Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Backend API** | ASP.NET Core 8 | REST API and MCP server |
| **Database** | MongoDB Atlas | Document storage and queries |
| **AI Service** | Perplexity AI (Sonar) | Natural language processing |
| **Frontend** | Angular 17 + TypeScript | Interactive chat interface |
| **Protocol** | Model Context Protocol | AI-tool communication standard |

## 🚦 Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- MongoDB Atlas account (or local MongoDB)
- Perplexity AI API key

### 1. Clone and Setup
```bash
git clone https://github.com/yourusername/agentic-mcp.git
cd agentic-mcp

# Copy environment configuration
cp .env.example .env
# Edit .env with your API keys and database connection
```

### 2. Configure Environment
Update `.env` with your credentials:
```env
PERPLEXITY_API_KEY=your_perplexity_api_key_here
MONGODB_CONNECTION_STRING=your_mongodb_atlas_connection_string
MONGODB_DATABASE_NAME=AgenticOrderingSystem
```

### 3. Start the System
```bash
# Make scripts executable
chmod +x *.sh

# Start all services (API + Angular UI)
./start-services.sh
```

### 4. Access the Application
- **Chat Interface**: http://localhost:4200
- **API Documentation**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/api/health

## 💬 Example Interactions

### Natural Language Queries
```
👤 User: "Show me orders for user_emp_john"
🤖 AI: Returns formatted list with 6 orders, status breakdown, and summaries

👤 User: "Why was order REJ-2025-07-0001 rejected?"
🤖 AI: Provides detailed rejection analysis with manager comments and next steps

👤 User: "What tools are available?"
🤖 AI: Lists all available MCP tools with descriptions
```

### Rich Response Formatting
Instead of raw JSON, users see:
```
📊 Tool 1 Results

Order Details: REJ-2025-07-0001
• User: John Doe (user_emp_john)
• Product: Cybersecurity Consulting Services
• Status: ❌ Rejected
• Priority: high

❌ Rejection Details:
• Rejected by: Alice Manager
• Reason: BUDGET CONSTRAINTS
• Comments: Security consulting costs ($40,000) exceed quarterly budget allocation...

⚡ Next Possible Actions: edit, resubmit, cancel
```

## 🔧 Development

### Project Structure
```
AgenticMCP/
├── src/AgenticOrderingSystem.API/     # .NET 8 Web API
│   ├── MCP/                           # Model Context Protocol implementation
│   ├── Controllers/                   # API endpoints
│   ├── Services/                      # Business logic
│   └── Models/                        # Data models
├── chat-ui/                           # Angular 17 frontend
│   ├── src/app/chat/                  # Chat component
│   └── src/app/services/              # API services
├── Database/                          # Database scripts and seeds
└── *.sh                              # Automation scripts
```

### Available Scripts
```bash
./start-services.sh     # Start API and Angular development servers
./stop-services.sh      # Stop all services
./test-endpoints.sh     # Test API endpoints
./test-mcp-endpoints.sh # Test MCP tool execution
```

### Testing
```bash
# Test basic order operations
curl -X GET "http://localhost:5000/api/orders/test-data"

# Test MCP chat functionality
curl -X POST "http://localhost:5000/api/mcp/chat" \
  -H "Content-Type: application/json" \
  -d '{"message": "Show me orders for user_emp_john"}'
```

## 📚 Key Technical Achievements

### 1. **Order Number Resolution**
The system intelligently handles both:
- **Order Numbers**: `REJ-2025-07-0001` (human-friendly)
- **Order IDs**: `4b602fef-3de4-4317-ac18-cb6e0c616a4c` (database GUIDs)

### 2. **Smart Parameter Validation**
- Custom `JsonElement` handling for flexible parameter types
- Automatic conversion between `string` and `List<string>` parameters
- Graceful error handling with user-friendly messages

### 3. **AI-Enhanced Responses**
- Context-aware analysis of business data
- Intelligent recommendations based on order status
- Natural language explanation of complex approval workflows

### 4. **Enterprise-Grade Architecture**
- Scalable MongoDB document storage
- Comprehensive approval workflow engine
- Real-time health monitoring and error tracking

## 🏗️ Architecture Highlights

### MCP Tool Execution Flow
```
User Query → AI Processing → Tool Parameter Extraction → Database Query → Formatted Response
```

### AI Integration Pattern
```
Natural Language → Perplexity AI → Tool Selection → Parameter Validation → Business Logic → User-Friendly Response
```

## 📈 Development Phases

- **✅ Phase A**: Basic .NET API with MongoDB integration
- **✅ Phase B**: Angular UI with real-time chat functionality  
- **✅ Phase C**: Complete AI integration with tool execution
- **🎯 Current**: Production-ready system with comprehensive features

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit your changes: `git commit -m 'Add amazing feature'`
4. Push to the branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙋‍♂️ Support

For questions or support:
- Open an issue on GitHub
- Check the [TESTING_GUIDE.md](TESTING_GUIDE.md) for troubleshooting
- Review the [PROJECT_ROADMAP.md](PROJECT_ROADMAP.md) for planned features

---

**Built with ❤️ using the Model Context Protocol**

*Demonstrating the future of AI-human interaction in enterprise applications*
