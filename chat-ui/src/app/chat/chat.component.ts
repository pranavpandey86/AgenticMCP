import { Component, OnInit, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { ChatService, ChatMessage, ToolResult } from '../services/chat.service';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css']
})
export class ChatComponent implements OnInit, AfterViewChecked {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;
  
  messages: ChatMessage[] = [];
  currentMessage: string = '';
  isTyping: boolean = false;
  isConnected: boolean = false;
  serverStatus: string = 'Connecting...';
  availableTools: number = 0;
  pendingConfirmation: { conversationId: string, requiresConfirmation: boolean } | null = null;

  // Quick action suggestions
  quickSuggestions = [
    "What tools are available?",
    "Show me orders for user_emp_john",
    "Why was order REJ-2025-07-0001 rejected?",
    "Analyze failure patterns for recent orders",
    "What are common rejection reasons?"
  ];

  constructor(private chatService: ChatService) {}

  ngOnInit(): void {
    this.checkServerHealth();
    this.addWelcomeMessage();
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  public checkServerHealth(): void {
    this.chatService.checkHealth().subscribe({
      next: (response) => {
        this.isConnected = response.success;
        this.serverStatus = response.data.status;
        this.availableTools = response.data.mcpServer.availableTools;
      },
      error: (error) => {
        console.error('Server health check failed:', error);
        this.isConnected = false;
        this.serverStatus = 'Disconnected';
      }
    });
  }

  private addWelcomeMessage(): void {
    const welcomeMessage: ChatMessage = {
      id: 'welcome-' + Date.now(),
      sender: 'assistant',
      message: `Welcome to the Agentic MCP Chat! 🤖\n\nI'm your AI assistant powered by the Model Context Protocol. I can help you with order management tasks using specialized tools.\n\nTry asking me about available tools or checking order information!`,
      timestamp: new Date(),
      confidence: 1.0
    };
    this.messages.push(welcomeMessage);
  }

  sendMessage(): void {
    if (!this.currentMessage.trim() || this.isTyping) {
      return;
    }

    // Add user message
    const userMessage: ChatMessage = {
      id: 'user-' + Date.now(),
      sender: 'user',
      message: this.currentMessage.trim(),
      timestamp: new Date()
    };
    this.messages.push(userMessage);

    // Store the message and clear input
    const messageToSend = this.currentMessage.trim();
    this.currentMessage = '';
    
    // Show typing indicator
    this.isTyping = true;

    // Check if this is a confirmation response
    if (this.pendingConfirmation && this.isConfirmationResponse(messageToSend)) {
      this.handleConfirmation(messageToSend);
      return;
    }

    // Clear any pending confirmation
    this.pendingConfirmation = null;

    // Send to AI service
    this.chatService.sendMessage(messageToSend).subscribe({
      next: (response) => {
        this.isTyping = false;
        
        // Agent response format is much simpler
        if (response.message) {
          const assistantMessage: ChatMessage = {
            id: 'assistant-' + Date.now(),
            sender: 'assistant',
            message: response.message,
            timestamp: new Date(),
            confidence: 0.9  // High confidence for agent responses
          };
          this.messages.push(assistantMessage);

          // Check if confirmation is required
          if (response.requiresConfirmation && response.conversationId) {
            this.pendingConfirmation = {
              conversationId: response.conversationId,
              requiresConfirmation: response.requiresConfirmation
            };
          }
        } else {
          this.addErrorMessage('Failed to get response from AI assistant.');
        }
      },
      error: (error) => {
        this.isTyping = false;
        console.error('Chat error:', error);
        this.addErrorMessage('Sorry, I encountered an error. Please try again.');
      }
    });
  }

  selectSuggestion(suggestion: string): void {
    this.currentMessage = suggestion;
    this.sendMessage();
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  resetChat(): void {
    this.messages = [];
    this.pendingConfirmation = null;
    this.chatService.resetSession();
    this.addWelcomeMessage();
  }

  private isConfirmationResponse(message: string): boolean {
    const lowerMessage = message.toLowerCase().trim();
    return lowerMessage === 'yes' || lowerMessage === 'y' || 
           lowerMessage === 'no' || lowerMessage === 'n' ||
           lowerMessage === 'confirm' || lowerMessage === 'cancel';
  }

  private handleConfirmation(message: string): void {
    if (!this.pendingConfirmation) return;

    const isConfirmed = ['yes', 'y', 'confirm'].includes(message.toLowerCase().trim());
    
    this.chatService.sendConfirmation(this.pendingConfirmation.conversationId, isConfirmed).subscribe({
      next: (response: any) => {
        this.isTyping = false;
        this.pendingConfirmation = null;
        
        if (response.message) {
          const assistantMessage: ChatMessage = {
            id: 'assistant-' + Date.now(),
            sender: 'assistant',
            message: response.message,
            timestamp: new Date(),
            confidence: 0.9
          };
          this.messages.push(assistantMessage);
        }
      },
      error: (error: any) => {
        this.isTyping = false;
        this.pendingConfirmation = null;
        console.error('Confirmation error:', error);
        this.addErrorMessage('Sorry, I encountered an error processing your confirmation. Please try again.');
      }
    });
  }

  private addErrorMessage(message: string): void {
    const errorMessage: ChatMessage = {
      id: 'error-' + Date.now(),
      sender: 'assistant',
      message: `❌ ${message}`,
      timestamp: new Date(),
      confidence: 0
    };
    this.messages.push(errorMessage);
  }

  private scrollToBottom(): void {
    try {
      if (this.messagesContainer) {
        this.messagesContainer.nativeElement.scrollTop = this.messagesContainer.nativeElement.scrollHeight;
      }
    } catch (err) {
      console.error('Scroll error:', err);
    }
  }

  formatTime(date: Date): string {
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  private formatToolResults(toolResults: any[], summary?: any): string {
    if (!toolResults || toolResults.length === 0) {
      return 'No tool results to display.';
    }

    let formattedMessage = '';

    // Add summary if available
    if (summary) {
      formattedMessage += `🔧 **Tool Execution Summary**\n`;
      formattedMessage += `✅ Successfully executed: ${summary.successfulTools}/${summary.toolsExecuted} tools\n\n`;
    }

    // Process each tool result
    toolResults.forEach((result, index) => {
      if (result.success && result.data) {
        formattedMessage += this.formatSuccessfulToolResult(result.data, index + 1);
      } else if (result.error) {
        formattedMessage += `❌ **Tool ${index + 1} Failed**\n`;
        formattedMessage += `Error: ${result.error.message || 'Unknown error'}\n\n`;
      }
    });

    return formattedMessage;
  }

  private formatSuccessfulToolResult(data: any, toolNumber: number): string {
    let formatted = `📊 **Tool ${toolNumber} Results**\n\n`;

    // Handle failure analysis results
    if (data.analysisContext && data.failureAnalysis) {
      formatted += `**🔍 Failure Analysis Report**\n\n`;
      formatted += `**Analysis Scope**: ${data.analysisContext.scope}\n`;
      formatted += `**Time Range**: ${data.analysisContext.timeRange}\n`;
      formatted += `**Orders Analyzed**: ${data.analysisContext.ordersAnalyzed}\n\n`;

      // Common failure reasons
      if (data.failureAnalysis.commonFailureReasons && data.failureAnalysis.commonFailureReasons.length > 0) {
        formatted += `**❌ Common Failure Reasons:**\n`;
        data.failureAnalysis.commonFailureReasons.slice(0, 3).forEach((reason: any, index: number) => {
          formatted += `${index + 1}. **${reason.reason.replace(/_/g, ' ').toUpperCase()}** (${reason.percentage.toFixed(1)}%)\n`;
          if (reason.exampleComments && reason.exampleComments.length > 0) {
            formatted += `   💬 "${reason.exampleComments[0]}"\n`;
          }
        });
        formatted += '\n';
      }

      // Success patterns
      if (data.successAnalysis && data.successAnalysis.successFactors) {
        formatted += `**✅ Success Factors:**\n`;
        data.successAnalysis.successFactors.slice(0, 3).forEach((factor: any) => {
          formatted += `• **${factor.factor}** (${factor.impact} impact)\n`;
          formatted += `  ${factor.description}\n`;
        });
        formatted += '\n';
      }

      // Recommendations
      if (data.recommendations) {
        if (data.recommendations.immediateActions && data.recommendations.immediateActions.length > 0) {
          formatted += `**⚡ Immediate Actions:**\n`;
          data.recommendations.immediateActions.forEach((action: string) => {
            formatted += `• ${action}\n`;
          });
          formatted += '\n';
        }

        if (data.recommendations.preventiveStrategies && data.recommendations.preventiveStrategies.length > 0) {
          formatted += `**🛡️ Prevention Strategies:**\n`;
          data.recommendations.preventiveStrategies.slice(0, 3).forEach((strategy: string) => {
            formatted += `• ${strategy}\n`;
          });
          formatted += '\n';
        }

        if (data.recommendations.confidenceScore) {
          const confidence = (data.recommendations.confidenceScore * 100).toFixed(0);
          formatted += `**🎯 Confidence Score**: ${confidence}%\n\n`;
        }
      }

      // Key insights
      if (data.insights && data.insights.keyFindings) {
        formatted += `**💡 Key Insights:**\n`;
        data.insights.keyFindings.forEach((finding: string) => {
          formatted += `• ${finding}\n`;
        });
        formatted += '\n';
      }
    }
    // Handle single order details (from get_order_details tool)
    else if (data.order && data.order.orderId) {
      const order = data.order;
      formatted += `**Order Details: ${order.orderNumber}**\n\n`;
      formatted += `• **ID**: ${order.orderId}\n`;
      formatted += `• **User**: ${order.userName} (${order.userId})\n`;
      formatted += `• **Product**: ${order.productName}\n`;
      formatted += `• **Status**: ${this.formatStatus(order.status)}\n`;
      formatted += `• **Priority**: ${order.priority}\n`;
      formatted += `• **Amount**: $${order.totalAmount} ${order.currency}\n`;
      formatted += `• **Created**: ${new Date(order.createdAt).toLocaleDateString()}\n`;
      
      if (order.businessJustification) {
        formatted += `• **Business Justification**: ${order.businessJustification}\n`;
      }
      
      // Handle rejection details specifically
      if (order.status === 'rejected' && order.approvalWorkflow) {
        formatted += `\n**❌ Rejection Details:**\n`;
        
        const rejectionAction = order.approvalWorkflow.history?.find((h: any) => h.action === 'reject');
        if (rejectionAction) {
          formatted += `• **Rejected by**: ${rejectionAction.userName}\n`;
          formatted += `• **Reason**: ${rejectionAction.reason.replace('_', ' ').toUpperCase()}\n`;
          formatted += `• **Comments**: ${rejectionAction.comments}\n`;
          formatted += `• **Date**: ${new Date(rejectionAction.timestamp).toLocaleDateString()}\n`;
        }
      }
      
      // Handle approval workflow
      if (order.approvalWorkflow) {
        formatted += `\n**📋 Approval Workflow:**\n`;
        formatted += `• **Current Step**: ${order.approvalWorkflow.currentStep}/${order.approvalWorkflow.totalSteps}\n`;
        formatted += `• **Status**: ${this.formatStatus(order.approvalWorkflow.status)}\n`;
        
        if (order.approvalWorkflow.approvers && order.approvalWorkflow.approvers.length > 0) {
          formatted += `• **Approvers**:\n`;
          order.approvalWorkflow.approvers.forEach((approver: any) => {
            formatted += `  - ${approver.fullName} (${approver.role}): ${this.formatStatus(approver.status)}\n`;
          });
        }
      }
      
      // Handle AI recommendations
      if (data.aiRecommendations && data.aiRecommendations.length > 0) {
        formatted += `\n**🤖 AI Recommendations:**\n`;
        data.aiRecommendations.forEach((rec: any) => {
          formatted += `• **${rec.title}**: ${rec.description}\n`;
        });
      }
      
      // Handle next actions
      if (data.metadata && data.metadata.nextPossibleActions) {
        formatted += `\n**⚡ Next Possible Actions**: ${data.metadata.nextPossibleActions.join(', ')}\n`;
      }
    }
    // Handle multiple orders (from get_user_orders tool)
    else if (data.orders && Array.isArray(data.orders)) {
      formatted += `**Orders Found: ${data.orders.length}**\n\n`;
      
      data.orders.forEach((order: any, index: number) => {
        formatted += `**${index + 1}. ${order.orderNumber || order.orderId}**\n`;
        formatted += `   • User: ${order.userName || order.userId}\n`;
        formatted += `   • Product: ${order.productName || order.productId}\n`;
        formatted += `   • Status: ${this.formatStatus(order.status)}\n`;
        formatted += `   • Priority: ${order.priority}\n`;
        formatted += `   • Created: ${new Date(order.createdAt).toLocaleDateString()}\n`;
        
        if (order.approvalWorkflow && order.approvalWorkflow.status) {
          formatted += `   • Approval: ${this.formatStatus(order.approvalWorkflow.status)}\n`;
        }
        
        formatted += '\n';
      });

      // Add summary if available
      if (data.summary) {
        formatted += `**Summary:**\n`;
        formatted += `• Total Orders: ${data.summary.totalOrders}\n`;
        
        if (data.summary.statusBreakdown) {
          formatted += `• Status Breakdown:\n`;
          Object.entries(data.summary.statusBreakdown).forEach(([status, count]) => {
            formatted += `  - ${this.capitalizeFirst(status)}: ${count}\n`;
          });
        }
      }
    }
    // Generic object display
    else {
      formatted += '```json\n' + JSON.stringify(data, null, 2) + '\n```\n';
    }

    return formatted + '\n';
  }

  private formatStatus(status: string): string {
    const statusMap: { [key: string]: string } = {
      'approved': '✅ Approved',
      'rejected': '❌ Rejected',
      'pending': '⏳ Pending',
      'submitted': '📤 Submitted',
      'draft': '📝 Draft',
      'in_progress': '⏳ In Progress',
      'completed': '✅ Completed'
    };
    return statusMap[status] || status.charAt(0).toUpperCase() + status.slice(1);
  }

  private capitalizeFirst(str: string): string {
    return str.charAt(0).toUpperCase() + str.slice(1);
  }

  formatMessage(message: string): string {
    // Simple formatting to handle basic markdown-like formatting
    return message
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.*?)\*/g, '<em>$1</em>')
      .replace(/`(.*?)`/g, '<code>$1</code>')
      .replace(/\n/g, '<br>');
  }
}
