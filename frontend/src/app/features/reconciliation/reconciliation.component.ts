import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { TenantService } from '../../core/services/tenant.service';
import { ReconciliationSummary } from '../../core/models/models';

@Component({
  selector: 'app-reconciliation',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="reconciliation-view animate-fade">
      <div class="page-header">
        <div>
          <h1>Multi-Rail Cross-Provider Reconciliation</h1>
          <p class="subtitle">Automated rule engine matching Daraja, Airtel, and Bank ledger items</p>
        </div>
        <div class="header-actions">
          <button (click)="triggerRun()" [disabled]="isRunning()" class="btn-primary">
            <span>{{ isRunning() ? '⏳ Running Matching Session...' : '⚡ Trigger Reconciliation Run' }}</span>
          </button>
        </div>
      </div>

      <!-- Live Session Status -->
      <div class="session-card glass-panel">
        <div class="session-header">
          <div>
            <div class="session-badge">SESSION: {{ recSummary()?.sessionId }}</div>
            <h2>Latest Matching Run: {{ recSummary()?.status }}</h2>
          </div>
          <div class="rate-box">
            <span class="rate-value">{{ recSummary()?.matchRatePercentage }}%</span>
            <span class="rate-label">MATCH RATE</span>
          </div>
        </div>

        <div class="session-metrics-grid">
          <div class="metric-item">
            <span class="label">TOTAL TRANSACTIONS</span>
            <span class="val">{{ recSummary()?.totalProcessed | number }}</span>
          </div>
          <div class="metric-item">
            <span class="label">SUCCESSFULLY MATCHED</span>
            <span class="val text-success">{{ recSummary()?.matchedCount | number }}</span>
          </div>
          <div class="metric-item">
            <span class="label">UNMATCHED EXCEPTIONS</span>
            <span class="val text-danger">{{ recSummary()?.unmatchedCount }}</span>
          </div>
          <div class="metric-item">
            <span class="label">FEE DISCREPANCIES</span>
            <span class="val text-warning">{{ recSummary()?.discrepancyCount }}</span>
          </div>
        </div>
      </div>

      <!-- Matching Rules Configuration -->
      <div class="rules-section glass-panel">
        <h3>Active Multi-Rail Matching Rules</h3>
        <div class="rules-grid">
          <div class="rule-card">
            <div class="rule-header">
              <span class="priority">Rule 1 (Exact Match)</span>
              <span class="status-active">Active</span>
            </div>
            <h4>Provider Reference Exact Key</h4>
            <p>Direct match between Daraja Transaction ID / Airtel Ref and Bank statement narration.</p>
            <div class="rule-meta">Confidence: 100% | Tolerance: KES 0.00 | Time Window: 48h</div>
          </div>

          <div class="rule-card">
            <div class="rule-header">
              <span class="priority">Rule 2 (Fuzzy Reference)</span>
              <span class="status-active">Active</span>
            </div>
            <h4>Invoice & BillRefNumber Heuristic</h4>
            <p>Normalises phone numbers (+254 / 07..), extracts invoice prefix, ignores leading zeros.</p>
            <div class="rule-meta">Confidence: 95% | Tolerance: KES 0.00 | Time Window: 72h</div>
          </div>

          <div class="rule-card">
            <div class="rule-header">
              <span class="priority">Rule 3 (Fee Delta Split)</span>
              <span class="status-active">Active</span>
            </div>
            <h4>Automatic Tariff & Excise Reconciliation</h4>
            <p>Detects standard Safaricom / Airtel excise fee differences (e.g. KES 250 or 20% excise duty).</p>
            <div class="rule-meta">Confidence: 90% | Tolerance: Standard Tariff Table</div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .reconciliation-view {
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

    .btn-primary {
      background-color: var(--primary);
      color: white;
      font-weight: 600;
      padding: 0.6rem 1.1rem;
      border-radius: var(--radius-md);
      font-size: 0.85rem;
    }

    .btn-primary:disabled {
      opacity: 0.7;
      cursor: not-allowed;
    }

    .session-card {
      padding: 1.5rem;
      border-radius: var(--radius-lg);
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .session-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .session-badge {
      font-size: 0.75rem;
      font-family: monospace;
      font-weight: 700;
      color: var(--safari-gold);
      margin-bottom: 0.25rem;
    }

    .session-header h2 {
      font-size: 1.4rem;
      font-weight: 800;
    }

    .rate-box {
      background: var(--bg-surface);
      border: 1px solid var(--border);
      padding: 0.85rem 1.5rem;
      border-radius: var(--radius-md);
      display: flex;
      flex-direction: column;
      align-items: center;
    }

    .rate-value {
      font-size: 1.8rem;
      font-weight: 800;
      color: var(--primary);
    }

    .rate-label {
      font-size: 0.65rem;
      font-weight: 700;
      color: var(--text-muted);
    }

    .session-metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
      gap: 1rem;
      padding-top: 1rem;
      border-top: 1px solid rgba(255, 255, 255, 0.05);
    }

    .metric-item {
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
    }

    .metric-item .label {
      font-size: 0.7rem;
      font-weight: 700;
      color: var(--text-muted);
    }

    .metric-item .val {
      font-size: 1.4rem;
      font-weight: 800;
    }

    .rules-section {
      padding: 1.5rem;
      border-radius: var(--radius-lg);
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .rules-section h3 {
      font-size: 1.15rem;
      font-weight: 700;
    }

    .rules-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1rem;
    }

    .rule-card {
      background-color: var(--bg-surface);
      border: 1px solid var(--border);
      padding: 1rem;
      border-radius: var(--radius-md);
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .rule-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .priority {
      font-size: 0.75rem;
      font-weight: 700;
      color: var(--safari-gold);
    }

    .status-active {
      font-size: 0.65rem;
      font-weight: 700;
      color: var(--success);
      background-color: var(--success-bg);
      padding: 2px 6px;
      border-radius: 4px;
    }

    .rule-card h4 {
      font-size: 0.95rem;
      font-weight: 700;
    }

    .rule-card p {
      font-size: 0.8rem;
      color: var(--text-secondary);
      line-height: 1.4;
    }

    .rule-meta {
      font-size: 0.7rem;
      color: var(--text-muted);
      margin-top: auto;
      padding-top: 0.5rem;
      border-top: 1px solid rgba(255, 255, 255, 0.05);
    }

    .text-success { color: var(--success); }
    .text-danger { color: var(--danger); }
    .text-warning { color: var(--warning); }
  `]
})
export class ReconciliationComponent implements OnInit {
  private apiService = inject(ApiService);
  tenantService = inject(TenantService);

  recSummary = signal<ReconciliationSummary | null>(null);
  isRunning = signal<boolean>(false);

  ngOnInit(): void {
    this.loadSummary();
  }

  loadSummary(): void {
    this.apiService.getReconciliationSummary().subscribe(res => this.recSummary.set(res));
  }

  triggerRun(): void {
    this.isRunning.set(true);
    this.apiService.triggerReconciliation().subscribe(() => {
      setTimeout(() => {
        this.isRunning.set(false);
        this.loadSummary();
      }, 1500);
    });
  }
}
