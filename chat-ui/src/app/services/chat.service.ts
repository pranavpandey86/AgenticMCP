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
  private currentToken: string | null = null;

  constructor(private http: HttpClient) {
    this.currentSessionId = this.generateSessionId();
    this.currentToken = localStorage.getItem('auth_token');
  }

  private generateSessionId(): string {
    return `chat-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  login(email: string, password: string): Observable<LoginResponse> {
    const request: LoginRequest = { email, password };
    return this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, request, this.getHttpOptions())
      .pipe(
        tap((response: LoginResponse) => {
          if (response.success) {
            this.currentToken = response.token;
            localStorage.setItem('auth_token', response.token);
            localStorage.setItem('user_id', response.userId);
            localStorage.setItem('user_name', response.fullName);
          }
        })
      );
  }

  logout(): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/logout`, {}, this.getAuthenticatedHttpOptions())
      .pipe(
        tap(() => {
          this.currentToken = null;
          localStorage.removeItem('auth_token');
          localStorage.removeItem('user_id');
          localStorage.removeItem('user_name');
        })
      );
  }

  isAuthenticated(): boolean {
    return this.currentToken !== null;
  }

  getCurrentUserId(): string | null {
    return localStorage.getItem('user_id');
  }

  getCurrentUserName(): string | null {
    return localStorage.getItem('user_name');
  }

  private getHttpOptions() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      })
    };
  }

  private getAuthenticatedHttpOptions() {
    const headers: any = {
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    };
    
    if (this.currentToken) {
      headers['Authorization'] = `Bearer ${this.currentToken}`;
    }
    
    return { headers: new HttpHeaders(headers) };
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
      userId: "mkt_david_designer"  // Use the test user who owns TEAM-FAIL-001
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
      confirmed: confirmed
    };

    return this.http.post<ConversationResponse>(
      `${this.apiUrl}/agent/confirm`, 
      request, 
      this.getAuthenticatedHttpOptions()
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
