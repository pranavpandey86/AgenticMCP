# Agentic MCP Project: Infrastructure, Flow, and Methods

## Infrastructure Overview
- **Backend:** .NET 8 Web API (AgenticOrderingSystem.API)
- **Frontend:** Angular (chat-ui)
- **Database:** MongoDB Atlas (cloud)
- **AI Integration:** Perplexity AI (for order analysis, recommendations)
- **Containerization:** Docker (optional, for deployment)

## Application Flow
1. **User interacts with chat UI** (Angular)
2. **Chat UI sends messages** to backend API (`/api/agent/chat`)
3. **AgentOrchestratorService** detects intent, routes to appropriate MCP tool
4. **MCP Tools** (GetOrderDetails, AnalyzeOrderFailures, UpdateOrderAgentTool, etc.) perform business logic
5. **Order/Team/Product data** is fetched from MongoDB
6. **AI-powered analysis** (Perplexity) provides recommendations for failed orders
7. **User confirms update**; order is updated and resubmitted
8. **All actions and state** are tracked in conversation context

## Key Methods & Responsibilities
- **AgentOrchestratorService.HandleChatMessageAsync:** Main entry for chat, intent detection, and tool routing
- **UpdateOrderAgentTool.ExecuteAsync:** Updates order with AI-suggested values, changes status, recalculates totals
- **AnalyzeOrderFailuresTool:** Compares failed order to team patterns, extracts AI recommendations
- **GetOrderDetailsTool:** Fetches order details by ID or order number
- **TeamBasedOrderSeeder:** Seeds realistic team orders for testing/analysis
- **ProductMockData:** Provides product catalog for order validation

## Instructions for Developers
- Use `/api/dev/seed` and `/api/dev/seed-team-orders` to seed data
- Use user IDs that match seeded orders (e.g., `mkt_david_designer` for TEAM-FAIL-001)
- To update orders, user must be the original requester
- For troubleshooting, check logs for authorization and product lookup errors

## Project Structure
- `src/AgenticOrderingSystem.API/` - .NET backend (controllers, services, MCP tools, data)
- `chat-ui/` - Angular frontend
- `Database/Seeds/` - Seed scripts and mock data

## Flow Diagram
```
User → Chat UI → API → AgentOrchestratorService → [MCP Tool] → MongoDB/AI → Response → Chat UI
```

---

# For full project structure and intent, see `README.md`.
