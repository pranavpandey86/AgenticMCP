import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

export interface ChatMessage {
  id: string;
  sender: 'user' | 'assistant';
  message: string;
  timestamp: Date;
  confidence?: number;
  intent?: string;
}

export interface ConversationRequest {
  sessionId: string;
  message: string;
}

export interface ConversationResponse {
  success?: boolean;  // For legacy compatibility
  message: string;
  conversationId: string;
  requiresConfirmation: boolean;
  data: {
    analysis?: any;
    can_update?: boolean;
    suggested_values?: any;
    // Legacy MCP fields for fallback
    conversationResponse?: {
      message: string;
      toolCalls?: any[];
      nextActions?: string[];
      confidence: number;
      intent?: string;
      conversationComplete: boolean;
      metadata: {
        processingTimeMs: number;
        model: string;
        tokensUsed: {
          inputTokens: number;
          outputTokens: number;
          totalTokens: number;
        };
        cost: number;
        requestId: string;
      };
    };
    toolResults?: ToolResult[];
    toolExecutionSummary?: {
      toolsExecuted: number;
      successfulTools: number;
      failedTools: number;
    };
    // Fallback for simple responses
    confidence?: number;
    intent?: string;
    metadata?: any;
  };
  timestamp: string;
}

export interface ToolResult {
  success: boolean;
  data: any;
  error?: any;
  metadata: {
    executionTime: number;
    toolVersion: string;
    requestId: string;
    timestamp: string;
  };
}

export interface MCPHealthResponse {
  success: boolean;
  data: {
    status: string;
    timestamp: string;
    mcpServer: {
      availableTools: number;
      toolCategories: number;
      version: string;
    };
    aiService: {
      status: string;
      provider: string;
    };
  };
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private readonly apiUrl = 'http://localhost:5001/api';
  private currentSessionId: string;
  private currentConversationId: string | null = null;

  constructor(private http: HttpClient) {
    this.currentSessionId = this.generateSessionId();
  }

  private generateSessionId(): string {
    return `chat-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  private getHttpOptions() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      })
    };
  }

  // Check if the MCP server is healthy
  checkHealth(): Observable<MCPHealthResponse> {
    return this.http.get<MCPHealthResponse>(`${this.apiUrl}/mcp/health`, this.getHttpOptions());
  }

  // Send a message to the AI assistant
  sendMessage(message: string): Observable<ConversationResponse> {
    const request = {
      message: message,
      conversationId: this.currentConversationId,  // null for first message
      userId: "demo-user-001"
    };

    return this.http.post<ConversationResponse>(
      `${this.apiUrl}/agent/chat`, 
      request, 
      this.getHttpOptions()
    ).pipe(
      tap((response: ConversationResponse) => {
        // Store the conversation ID for subsequent messages
        if (response.conversationId) {
          this.currentConversationId = response.conversationId;
        }
      })
    );
  }

  // Send a confirmation response to the agent
  sendConfirmation(conversationId: string, confirmed: boolean): Observable<ConversationResponse> {
    const request = {
      conversationId: conversationId,
      confirmed: confirmed,
      userId: "demo-user-001"
    };

    return this.http.post<ConversationResponse>(
      `${this.apiUrl}/agent/confirm`, 
      request, 
      this.getHttpOptions()
    );
  }

  // Get available tools (for displaying capabilities)
  getAvailableTools(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/mcp/tools`, this.getHttpOptions());
  }

  // Execute a specific tool
  executeTool(toolName: string, parameters: any): Observable<any> {
    const request = {
      toolName: toolName,
      parameters: parameters
    };

    return this.http.post<any>(
      `${this.apiUrl}/mcp/tools/execute`, 
      request, 
      this.getHttpOptions()
    );
  }

  // Reset the conversation session
  resetSession(): void {
    this.currentSessionId = this.generateSessionId();
    this.currentConversationId = null;
  }

  // Get the current session ID
  getSessionId(): string {
    return this.currentSessionId;
  }
}
