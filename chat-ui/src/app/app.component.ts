import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  template: `
    <div class="container">
      <app-chat></app-chat>
    </div>
  `,
  styles: []
})
export class AppComponent {
  title = 'Agentic MCP Chat';
}
