import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { TenantService } from '../../core/services/tenant.service';
import { UnmatchedTransaction } from '../../core/models/models';

@Component({
  selector: 'app-exceptions',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="exceptions-view animate-fade">
      <div class="page-header">
        <div>
          <h1>Reconciliation Exceptions & Unmatched Queue</h1>
          <p class="subtitle">Actionable items requiring human approval or force-matching</p>
        </div>
        <div class="header-actions">
          <span class="badge-count">{{ exceptions().length }} Pending Review</span>
        </div>
      </div>

      <!-- Exceptions List -->
      <div class="exceptions-container">
        @for (item of exceptions(); track item.id) {
          <div class="exception-card glass-panel">
            <div class="card-left">
              <div class="item-header">
                <span class="provider-tag">{{ item.provider }}</span>
                <span class="ref-code">{{ item.reference }}</span>
                <span class="timestamp">{{ item.occurredAtUtc | date:'medium' }}</span>
              </div>
              <div class="counterparty-row">
                <strong>Counterparty:</strong> {{ item.counterparty }}
              </div>
              <div class="reason-row">
                <span class="reason-label">DISCREPANCY:</span>
                <span class="reason-text">{{ item.discrepancyReason }}</span>
              </div>

              @if (item.suggestedMatch) {
                <div class="suggestion-box">
                  <div class="suggestion-header">
                    <span>✨ AI-Assisted Match Candidate</span>
                    <span class="confidence">Confidence: {{ (item.suggestedMatch.confidenceScore * 100) }}%</span>
                  </div>
                  <div class="suggestion-body">
                    Target: <strong>{{ item.suggestedMatch.candidateRef }}</strong>
                    @if (item.suggestedMatch.amountDifference > 0) {
                      <span class="delta-tag">(KES {{ item.suggestedMatch.amountDifference }} tariff delta)</span>
                    }
                  </div>
                </div>
              }
            </div>

            <div class="card-right">
              <div class="amount-tag">KES {{ item.amount | number:'1.2-2' }}</div>
              <div class="actions">
                @if (item.suggestedMatch) {
                  <button (click)="approveMatch(item.id)" class="btn-approve">Approve Match</button>
                }
                <button (click)="manualResolve(item.id)" class="btn-manual">Manual Note & Clear</button>
              </div>
            </div>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .exceptions-view {
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

    .badge-count {
      background-color: var(--danger-bg);
      color: var(--danger);
      font-weight: 700;
      padding: 0.4rem 0.85rem;
      border-radius: var(--radius-md);
      font-size: 0.85rem;
    }

    .exceptions-container {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .exception-card {
      padding: 1.25rem 1.5rem;
      border-radius: var(--radius-lg);
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1.5rem;
    }

    .card-left {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      flex: 1;
    }

    .item-header {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .provider-tag {
      font-size: 0.75rem;
      font-weight: 700;
      color: var(--safari-gold);
      background: var(--safari-gold-light);
      padding: 2px 6px;
      border-radius: 4px;
    }

    .ref-code {
      font-family: monospace;
      font-weight: 700;
      font-size: 1rem;
    }

    .timestamp {
      font-size: 0.75rem;
      color: var(--text-muted);
    }

    .counterparty-row {
      font-size: 0.85rem;
      color: var(--text-secondary);
    }

    .reason-row {
      display: flex;
      align-items: baseline;
      gap: 0.5rem;
      font-size: 0.85rem;
    }

    .reason-label {
      font-size: 0.7rem;
      font-weight: 800;
      color: var(--danger);
    }

    .reason-text {
      color: #fca5a5;
    }

    .suggestion-box {
      background-color: rgba(5, 150, 105, 0.1);
      border: 1px dashed var(--primary);
      border-radius: var(--radius-md);
      padding: 0.6rem 0.85rem;
      margin-top: 0.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .suggestion-header {
      display: flex;
      justify-content: space-between;
      font-size: 0.75rem;
      font-weight: 700;
      color: var(--primary);
    }

    .suggestion-body {
      font-size: 0.85rem;
    }

    .delta-tag {
      font-size: 0.75rem;
      color: var(--warning);
      margin-left: 0.4rem;
    }

    .card-right {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 0.75rem;
    }

    .amount-tag {
      font-size: 1.4rem;
      font-weight: 800;
      color: var(--text-primary);
    }

    .actions {
      display: flex;
      gap: 0.5rem;
    }

    .btn-approve {
      background-color: var(--primary);
      color: white;
      font-weight: 600;
      font-size: 0.8rem;
      padding: 0.5rem 0.9rem;
      border-radius: var(--radius-md);
    }

    .btn-approve:hover {
      background-color: var(--primary-hover);
    }

    .btn-manual {
      background-color: var(--bg-surface);
      border: 1px solid var(--border);
      color: var(--text-secondary);
      font-size: 0.8rem;
      padding: 0.5rem 0.9rem;
      border-radius: var(--radius-md);
    }

    .btn-manual:hover {
      background-color: var(--bg-card);
      color: var(--text-primary);
    }
  `]
})
export class ExceptionsComponent implements OnInit {
  private apiService = inject(ApiService);
  tenantService = inject(TenantService);

  exceptions = signal<UnmatchedTransaction[]>([]);

  ngOnInit(): void {
    this.apiService.getUnmatchedTransactions().subscribe(list => this.exceptions.set(list));
  }

  approveMatch(id: string): void {
    this.exceptions.update(items => items.filter(i => i.id !== id));
  }

  manualResolve(id: string): void {
    const notes = prompt('Enter resolution notes for audit trail:');
    if (notes) {
      this.apiService.resolveException(id, notes).subscribe(() => {
        this.exceptions.update(items => items.filter(i => i.id !== id));
      });
    }
  }
}
