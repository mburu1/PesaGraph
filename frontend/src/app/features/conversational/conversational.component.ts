import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { TenantService } from '../../core/services/tenant.service';
import { WhatsAppMessage } from '../../core/models/models';

@Component({
  selector: 'app-conversational',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="conversational-view animate-fade">
      <div class="page-header">
        <div>
          <h1>WhatsApp Conversational Cockpit (Simulated Interface)</h1>
          <p class="subtitle">Real-time KRA/Banking style operational bot for Kenyan SME operators</p>
        </div>
        <div class="status-indicator-box">
          <span class="live-dot"></span>
          <span>Meta Cloud API: <strong>Connected</strong></span>
        </div>
      </div>

      <div class="chat-container glass-panel">
        <!-- Phone Frame -->
        <div class="phone-screen">
          <div class="whatsapp-header">
            <div class="bot-avatar">🤖</div>
            <div class="bot-info">
              <span class="bot-name">PesaGraph Operations Assistant</span>
              <span class="bot-status">online • Kenya Multi-Rail Bot</span>
            </div>
          </div>

          <!-- Message Bubbles Stream -->
          <div class="messages-area">
            @for (msg of messages(); track msg.id) {
              <div class="bubble-row" [ngClass]="msg.sender">
                <div class="bubble" [ngClass]="msg.sender">
                  <div class="message-text" [innerHTML]="formatMessage(msg.text)"></div>
                  <span class="timestamp">{{ msg.timestamp | date:'shortTime' }}</span>
                </div>
              </div>
            }
          </div>

          <!-- Quick Command Shortcuts -->
          <div class="quick-commands">
            <button (click)="sendQuickCommand('float')">float</button>
            <button (click)="sendQuickCommand('unmatched')">unmatched</button>
            <button (click)="sendQuickCommand('resolve RKA991823H')">resolve RKA991823H</button>
            <button (click)="sendQuickCommand('summary')">summary</button>
            <button (click)="sendQuickCommand('help')">help</button>
          </div>

          <!-- Input bar -->
          <div class="chat-input-bar">
            <input
              type="text"
              [(ngModel)]="userInput"
              (keydown.enter)="sendMessage()"
              placeholder="Send WhatsApp message (e.g. float, unmatched, resolve)..."
            />
            <button (click)="sendMessage()" class="btn-send">
              <span>Send</span>
            </button>
          </div>
        </div>

        <!-- Documentation & Explanatory Sidebar -->
        <div class="bot-guide">
          <h3>Kenya-First Conversational Operations</h3>
          <p>
            Kenyan operators, SACCO managers, and super-agents trust WhatsApp and SMS for day-to-day work.
            Instead of opening an ERP portal, they send commands from the road to check balances, review unmatched transactions, or approve reconciliation exceptions.
          </p>

          <div class="guide-box">
            <h4>Available Commands</h4>
            <ul>
              <li><strong>float</strong>: Query live consolidated float across Daraja, Airtel Money, and bank pools.</li>
              <li><strong>unmatched</strong>: Pull top pending discrepancy items from the exception queue.</li>
              <li><strong>resolve &lt;REF&gt;</strong>: Approves a suggested match, posts the journal entry, and audits the operator.</li>
              <li><strong>summary</strong>: Generates an on-demand operations digest for management.</li>
            </ul>
          </div>

          <div class="guide-box">
            <h4>Webhook Route Reference</h4>
            <div class="code-snippet">
              GET /api/v1/conversationalwebhook/whatsapp<br/>
              POST /api/v1/conversationalwebhook/whatsapp
            </div>
            <span class="note">Verified via Meta Cloud API challenge token handshake.</span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .conversational-view {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .page-header h1 {
      font-size: 1.6rem;
      font-weight: 800;
    }

    .subtitle {
      color: var(--text-secondary);
      font-size: 0.9rem;
      margin-top: 0.2rem;
    }

    .status-indicator-box {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.85rem;
      background: var(--bg-surface);
      border: 1px solid var(--border);
      padding: 0.4rem 0.85rem;
      border-radius: var(--radius-md);
    }

    .live-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background-color: var(--success);
      box-shadow: 0 0 8px var(--success);
    }

    .chat-container {
      display: grid;
      grid-template-columns: 550px 1fr;
      gap: 2rem;
      padding: 1.5rem;
      border-radius: var(--radius-xl);
    }

    .phone-screen {
      background-color: #0b141a;
      border-radius: var(--radius-lg);
      border: 1px solid #222e35;
      display: flex;
      flex-direction: column;
      height: 600px;
      overflow: hidden;
    }

    .whatsapp-header {
      background-color: #202c33;
      padding: 0.75rem 1rem;
      display: flex;
      align-items: center;
      gap: 0.75rem;
      border-bottom: 1px solid #2a3942;
    }

    .bot-avatar {
      width: 38px;
      height: 38px;
      border-radius: 50%;
      background-color: #00a884;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.2rem;
    }

    .bot-info {
      display: flex;
      flex-direction: column;
    }

    .bot-name {
      font-size: 0.9rem;
      font-weight: 600;
      color: #e9edef;
    }

    .bot-status {
      font-size: 0.7rem;
      color: #8696a0;
    }

    .messages-area {
      flex: 1;
      overflow-y: auto;
      padding: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      background-image: radial-gradient(#182229 1px, transparent 1px);
      background-size: 16px 16px;
    }

    .bubble-row {
      display: flex;
      width: 100%;
    }

    .bubble-row.user {
      justify-content: flex-end;
    }

    .bubble-row.agent {
      justify-content: flex-start;
    }

    .bubble {
      max-width: 82%;
      padding: 0.65rem 0.85rem;
      border-radius: 8px;
      font-size: 0.85rem;
      line-height: 1.4;
      position: relative;
    }

    .bubble.user {
      background-color: #005c4b;
      color: #e9edef;
      border-top-right-radius: 2px;
    }

    .bubble.agent {
      background-color: #202c33;
      color: #d1d7db;
      border-top-left-radius: 2px;
    }

    .message-text {
      white-space: pre-line;
    }

    .timestamp {
      display: block;
      font-size: 0.65rem;
      color: #8696a0;
      text-align: right;
      margin-top: 0.25rem;
    }

    .quick-commands {
      background-color: #111b21;
      padding: 0.5rem 0.75rem;
      display: flex;
      gap: 0.4rem;
      overflow-x: auto;
      border-top: 1px solid #222e35;
    }

    .quick-commands button {
      background-color: #202c33;
      color: #00a884;
      font-size: 0.75rem;
      font-weight: 600;
      padding: 3px 8px;
      border-radius: 4px;
      white-space: nowrap;
    }

    .quick-commands button:hover {
      background-color: #2a3942;
    }

    .chat-input-bar {
      background-color: #202c33;
      padding: 0.6rem 0.75rem;
      display: flex;
      gap: 0.5rem;
    }

    .chat-input-bar input {
      flex: 1;
      background-color: #2a3942;
      border: none;
      color: #e9edef;
      font-size: 0.85rem;
    }

    .btn-send {
      background-color: #00a884;
      color: white;
      font-weight: 600;
      padding: 0 1rem;
      border-radius: var(--radius-md);
      font-size: 0.85rem;
    }

    .bot-guide {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    .bot-guide h3 {
      font-size: 1.2rem;
      font-weight: 700;
    }

    .bot-guide p {
      font-size: 0.9rem;
      color: var(--text-secondary);
      line-height: 1.5;
    }

    .guide-box {
      background: var(--bg-surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-md);
      padding: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .guide-box h4 {
      font-size: 0.95rem;
      font-weight: 700;
    }

    .guide-box ul {
      padding-left: 1.2rem;
      font-size: 0.85rem;
      color: var(--text-secondary);
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
    }

    .code-snippet {
      font-family: monospace;
      font-size: 0.8rem;
      background-color: var(--bg-card);
      padding: 0.5rem;
      border-radius: 4px;
      color: #a7f3d0;
    }

    .note {
      font-size: 0.75rem;
      color: var(--text-muted);
    }
  `]
})
export class ConversationalComponent {
  private apiService = inject(ApiService);
  tenantService = inject(TenantService);

  userInput = '';

  messages = signal<WhatsAppMessage[]>([
    {
      id: '1',
      sender: 'agent',
      text: 'Jambo! 👋 I am your PesaGraph Operations Assistant.\nSend *float*, *unmatched*, *resolve <ref>*, or *summary* to manage your accounts.',
      timestamp: new Date().toISOString()
    }
  ]);

  sendMessage(): void {
    if (!this.userInput.trim()) return;

    const cmd = this.userInput.trim();
    this.messages.update(list => [
      ...list,
      {
        id: crypto.randomUUID(),
        sender: 'user',
        text: cmd,
        timestamp: new Date().toISOString()
      }
    ]);

    this.userInput = '';

    this.apiService.executeConversationalCommand(cmd).subscribe(reply => {
      this.messages.update(list => [
        ...list,
        {
          id: crypto.randomUUID(),
          sender: 'agent',
          text: reply,
          timestamp: new Date().toISOString()
        }
      ]);
    });
  }

  sendQuickCommand(cmd: string): void {
    this.userInput = cmd;
    this.sendMessage();
  }

  formatMessage(text: string): string {
    return text
      .replace(/\*(.*?)\*/g, '<strong>$1</strong>')
      .replace(/_(.*?)_/g, '<em>$1</em>');
  }
}
