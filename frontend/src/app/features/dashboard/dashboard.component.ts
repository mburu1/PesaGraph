import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { TenantService } from '../../core/services/tenant.service';
import { CanonicalTransaction, LiquiditySummary, ReconciliationSummary } from '../../core/models/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="dashboard-view animate-fade">
      <!-- Header Banner -->
      <div class="page-header">
        <div>
          <h1>Operations Brain & Liquidity Radar</h1>
          <p class="subtitle">Multi-rail synchronization for {{ tenantService.currentTenant().name }}</p>
        </div>
        <div class="header-actions">
          <button (click)="loadData()" class="btn-secondary">
            <span>🔄 Refresh Feeds</span>
          </button>
          <a routerLink="/reconciliation" class="btn-primary">
            <span>⚡ Run Auto-Reconciliation</span>
          </a>
        </div>
      </div>

      <!-- KPI Summary Cards -->
      <div class="kpi-grid">
        <div class="kpi-card glass-panel">
          <div class="kpi-header">
            <span class="kpi-label">TOTAL MULTI-RAIL FLOAT</span>
            <span class="icon-tag">💰</span>
          </div>
          <div class="kpi-value">KES {{ (liquidity()?.totalLiquidity || 0) | number:'1.0-0' }}</div>
          <div class="kpi-footer">
            <span class="badge-success">Across 5 Accounts</span>
            <span class="kpi-subtext">M-Pesa, Airtel & Banks</span>
          </div>
        </div>

        <div class="kpi-card glass-panel">
          <div class="kpi-header">
            <span class="kpi-label">AUTO-MATCH RATE</span>
            <span class="icon-tag">🎯</span>
          </div>
          <div class="kpi-value">{{ recSummary()?.matchRatePercentage || 0 }}%</div>
          <div class="kpi-footer">
            <span class="badge-success">{{ recSummary()?.matchedCount || 0 }} Matched</span>
            <span class="kpi-subtext">21 Pending manual review</span>
          </div>
        </div>

        <div class="kpi-card glass-panel">
          <div class="kpi-header">
            <span class="kpi-label">PENDING EXCEPTIONS</span>
            <span class="icon-tag">⚠️</span>
          </div>
          <div class="kpi-value text-danger">KES {{ (recSummary()?.unmatchedVolume || 0) | number:'1.0-0' }}</div>
          <div class="kpi-footer">
            <span class="badge-danger">21 Unmatched Items</span>
            <a routerLink="/exceptions" class="link-action">Inspect Queue &rarr;</a>
          </div>
        </div>

        <div class="kpi-card glass-panel">
          <div class="kpi-header">
            <span class="kpi-label">WHATSAPP AGENT STATUS</span>
            <span class="icon-tag">📱</span>
          </div>
          <div class="kpi-value text-success">CONNECTED</div>
          <div class="kpi-footer">
            <span class="badge-success">Meta Cloud API Live</span>
            <a routerLink="/conversational" class="link-action">Open Bot &rarr;</a>
          </div>
        </div>
      </div>

      <!-- Float Cockpit Strip -->
      <div class="section-container">
        <div class="section-title-bar">
          <h2>Float & Liquidity Positions (Live Rails)</h2>
          <a routerLink="/liquidity" class="link-action">Full Float Cockpit &rarr;</a>
        </div>

        <div class="float-strips">
          @for (pos of liquidity()?.positions; track pos.accountNumber) {
            <div class="float-item glass-panel" [ngClass]="pos.status.toLowerCase()">
              <div class="float-item-header">
                <span class="provider-name">{{ pos.provider }}</span>
                <span class="status-pill" [ngClass]="pos.status.toLowerCase()">{{ pos.status }}</span>
              </div>
              <div class="account-name">{{ pos.accountName }}</div>
              <div class="balance-amount">KES {{ pos.currentBalance | number:'1.0-0' }}</div>
              <div class="buffer-bar">
                <div class="buffer-fill" [style.width.%]="calcPercentage(pos.currentBalance, pos.optimalThreshold)"></div>
              </div>
              <div class="threshold-info">Min: KES {{ pos.minimumThreshold | number:'1.0-0' }}</div>
            </div>
          }
        </div>
      </div>

      <!-- Live Normalised Transaction Stream -->
      <div class="section-container">
        <div class="section-title-bar">
          <h2>Canonical Ingestion Stream (Real-Time Ingestion)</h2>
          <span class="stream-pulse"><span class="pulse-dot"></span> Ingesting Daraja & Airtel webhooks</span>
        </div>

        <div class="table-card glass-panel">
          <table class="data-table">
            <thead>
              <tr>
                <th>TIMESTAMP</th>
                <th>PROVIDER</th>
                <th>CHANNEL</th>
                <th>PROVIDER REF</th>
                <th>EXT REF</th>
                <th>AMOUNT</th>
                <th>STATUS</th>
              </tr>
            </thead>
            <tbody>
              @for (tx of transactions(); track tx.id) {
                <tr>
                  <td>{{ tx.occurredAtUtc | date:'shortTime' }}</td>
                  <td>
                    <span class="provider-pill" [ngClass]="tx.provider.toLowerCase()">{{ tx.provider }}</span>
                  </td>
                  <td>{{ tx.channel }}</td>
                  <td class="code-ref">{{ tx.providerReference }}</td>
                  <td>{{ tx.externalReference || '—' }}</td>
                  <td [class.text-success]="tx.type === 'Inflow'" [class.text-danger]="tx.type === 'Outflow'" class="font-bold">
                    {{ tx.type === 'Inflow' ? '+' : '-' }} KES {{ tx.amount | number:'1.2-2' }}
                  </td>
                  <td>
                    <span class="tx-status" [ngClass]="tx.status.toLowerCase()">{{ tx.status }}</span>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-view {
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
      letter-spacing: -0.02em;
    }

    .subtitle {
      color: var(--text-secondary);
      font-size: 0.9rem;
      margin-top: 0.2rem;
    }

    .header-actions {
      display: flex;
      gap: 0.75rem;
    }

    .btn-primary {
      background-color: var(--primary);
      color: white;
      font-weight: 600;
      padding: 0.6rem 1.1rem;
      border-radius: var(--radius-md);
      font-size: 0.85rem;
      display: flex;
      align-items: center;
      gap: 0.4rem;
    }

    .btn-primary:hover {
      background-color: var(--primary-hover);
    }

    .btn-secondary {
      background-color: var(--bg-card);
      border: 1px solid var(--border);
      color: var(--text-primary);
      font-weight: 600;
      padding: 0.6rem 1.1rem;
      border-radius: var(--radius-md);
      font-size: 0.85rem;
    }

    .btn-secondary:hover {
      background-color: var(--bg-card-hover);
    }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      gap: 1rem;
    }

    .kpi-card {
      padding: 1.25rem;
      border-radius: var(--radius-lg);
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .kpi-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .kpi-label {
      font-size: 0.7rem;
      font-weight: 700;
      color: var(--text-muted);
      letter-spacing: 0.05em;
    }

    .icon-tag {
      font-size: 1.1rem;
    }

    .kpi-value {
      font-size: 1.7rem;
      font-weight: 800;
      letter-spacing: -0.02em;
    }

    .kpi-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.75rem;
      border-top: 1px solid rgba(255, 255, 255, 0.05);
      padding-top: 0.6rem;
    }

    .badge-success {
      background-color: var(--success-bg);
      color: var(--success);
      padding: 2px 7px;
      border-radius: 4px;
      font-weight: 600;
    }

    .badge-danger {
      background-color: var(--danger-bg);
      color: var(--danger);
      padding: 2px 7px;
      border-radius: 4px;
      font-weight: 600;
    }

    .link-action {
      color: var(--primary);
      font-weight: 600;
      font-size: 0.8rem;
    }

    .section-container {
      display: flex;
      flex-direction: column;
      gap: 0.85rem;
    }

    .section-title-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .section-title-bar h2 {
      font-size: 1.15rem;
      font-weight: 700;
    }

    .stream-pulse {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.8rem;
      color: var(--text-secondary);
    }

    .pulse-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background-color: var(--primary);
      box-shadow: 0 0 8px var(--primary);
    }

    .float-strips {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 0.85rem;
    }

    .float-item {
      padding: 1rem;
      border-radius: var(--radius-md);
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
    }

    .float-item-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.75rem;
    }

    .provider-name {
      font-weight: 700;
      color: var(--text-secondary);
    }

    .status-pill {
      font-size: 0.65rem;
      font-weight: 700;
      padding: 2px 6px;
      border-radius: 4px;
    }

    .status-pill.healthy { background: var(--success-bg); color: var(--success); }
    .status-pill.surplus { background: var(--info-bg); color: var(--info); }
    .status-pill.low { background: var(--warning-bg); color: var(--warning); }
    .status-pill.critical { background: var(--danger-bg); color: var(--danger); }

    .account-name {
      font-size: 0.85rem;
      font-weight: 600;
      color: var(--text-primary);
    }

    .balance-amount {
      font-size: 1.2rem;
      font-weight: 800;
      margin-top: 0.2rem;
    }

    .buffer-bar {
      height: 5px;
      background-color: var(--border);
      border-radius: 3px;
      overflow: hidden;
      margin: 0.4rem 0 0.2rem 0;
    }

    .buffer-fill {
      height: 100%;
      background-color: var(--primary);
      border-radius: 3px;
    }

    .threshold-info {
      font-size: 0.7rem;
      color: var(--text-muted);
    }

    .table-card {
      border-radius: var(--radius-lg);
      overflow: hidden;
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;
      text-align: left;
      font-size: 0.85rem;
    }

    .data-table th {
      background-color: var(--bg-surface);
      color: var(--text-muted);
      font-weight: 700;
      font-size: 0.7rem;
      letter-spacing: 0.05em;
      padding: 0.85rem 1rem;
      border-bottom: 1px solid var(--border);
    }

    .data-table td {
      padding: 0.85rem 1rem;
      border-bottom: 1px solid var(--border-light);
    }

    .data-table tr:hover {
      background-color: rgba(255, 255, 255, 0.02);
    }

    .provider-pill {
      font-weight: 600;
      font-size: 0.75rem;
      padding: 3px 8px;
      border-radius: 4px;
    }

    .provider-pill.m-pesa { background: rgba(5, 150, 105, 0.2); color: #34d399; }
    .provider-pill.airtelmoney { background: rgba(239, 68, 68, 0.2); color: #f87171; }
    .provider-pill.bank { background: rgba(59, 130, 246, 0.2); color: #60a5fa; }

    .code-ref {
      font-family: monospace;
      font-weight: 600;
      color: var(--text-secondary);
    }

    .tx-status {
      font-size: 0.7rem;
      font-weight: 700;
      padding: 2px 7px;
      border-radius: 4px;
    }

    .tx-status.reconciled { background: var(--success-bg); color: var(--success); }
    .tx-status.normalised { background: var(--info-bg); color: var(--info); }
    .tx-status.disputed { background: var(--danger-bg); color: var(--danger); }

    .font-bold { font-weight: 700; }
    .text-success { color: var(--success); }
    .text-danger { color: var(--danger); }
  `]
})
export class DashboardComponent implements OnInit {
  private apiService = inject(ApiService);
  tenantService = inject(TenantService);

  transactions = signal<CanonicalTransaction[]>([]);
  liquidity = signal<LiquiditySummary | null>(null);
  recSummary = signal<ReconciliationSummary | null>(null);

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.apiService.getRecentTransactions().subscribe(txs => this.transactions.set(txs));
    this.apiService.getLiquiditySummary().subscribe(l => this.liquidity.set(l));
    this.apiService.getReconciliationSummary().subscribe(r => this.recSummary.set(r));
  }

  calcPercentage(val: number, max: number): number {
    if (!max || max <= 0) return 100;
    return Math.min(100, Math.round((val / max) * 100));
  }
}
import { signal } from '@angular/core';
