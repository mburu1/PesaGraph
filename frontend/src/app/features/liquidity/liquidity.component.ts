import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { TenantService } from '../../core/services/tenant.service';
import { LiquiditySummary, FloatPosition } from '../../core/models/models';

@Component({
  selector: 'app-liquidity',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="liquidity-view animate-fade">
      <div class="page-header">
        <div>
          <h1>Multi-Rail Liquidity & Float Cockpit</h1>
          <p class="subtitle">Real-time float positions across M-Pesa, Airtel Money, and bank accounts</p>
        </div>
        <div class="header-actions">
          <button (click)="refresh()" class="btn-secondary">🔄 Refresh Balances</button>
          <button (click)="simulateRebalance()" class="btn-primary">⚡ Execute Recommended Rebalance</button>
        </div>
      </div>

      <!-- Liquidity Health Banner -->
      @if (liquidity()?.lowFloatCount! > 0) {
        <div class="alert-banner warning">
          <span class="alert-icon">⚠️</span>
          <div class="alert-text">
            <strong>Low Float Warning Detected:</strong>
            <span> Airtel Money float (KES 85,500) has breached the KES 100,000 threshold. Sweep recommended.</span>
          </div>
        </div>
      }

      <!-- Rebalance Recommendations -->
      <div class="recommendations-box glass-panel">
        <div class="rec-title">
          <span>💡</span>
          <h3>Intelligent Float Balancing Suggestions</h3>
        </div>
        <div class="rec-list">
          @for (rec of liquidity()?.recommendations; track rec) {
            <div class="rec-card">
              <span class="bullet">👉</span>
              <span>{{ rec }}</span>
            </div>
          }
        </div>
      </div>

      <!-- Float Position Cards Grid -->
      <div class="positions-grid">
        @for (pos of liquidity()?.positions; track pos.accountNumber) {
          <div class="position-card glass-panel" [ngClass]="pos.status.toLowerCase()">
            <div class="card-top">
              <div class="provider-badge">{{ pos.provider }}</div>
              <div class="status-pill" [ngClass]="pos.status.toLowerCase()">{{ pos.status }}</div>
            </div>
            
            <h3 class="account-title">{{ pos.accountName }}</h3>
            <div class="account-num">{{ pos.accountNumber }}</div>
            
            <div class="balance-display">
              <span class="currency">KES</span>
              <span class="amount">{{ pos.currentBalance | number:'1.0-0' }}</span>
            </div>

            <div class="progress-section">
              <div class="progress-labels">
                <span>Min Buffer: KES {{ pos.minimumThreshold | number:'1.0-0' }}</span>
                <span>Optimal: KES {{ pos.optimalThreshold | number:'1.0-0' }}</span>
              </div>
              <div class="progress-track">
                <div class="progress-fill" [style.width.%]="calcPercentage(pos.currentBalance, pos.optimalThreshold)"></div>
              </div>
            </div>

            <div class="card-footer">
              <span class="timestamp">Updated just now</span>
              <button class="btn-action">Rebalance &rarr;</button>
            </div>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .liquidity-view {
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

    .alert-banner {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.85rem 1.25rem;
      border-radius: var(--radius-md);
      font-size: 0.85rem;
    }

    .alert-banner.warning {
      background-color: rgba(245, 158, 11, 0.15);
      border: 1px solid var(--warning);
      color: #fbbf24;
    }

    .recommendations-box {
      padding: 1.25rem;
      border-radius: var(--radius-lg);
      display: flex;
      flex-direction: column;
      gap: 0.85rem;
    }

    .rec-title {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .rec-title h3 {
      font-size: 1rem;
      font-weight: 700;
    }

    .rec-list {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .rec-card {
      background-color: var(--bg-surface);
      border: 1px solid var(--border);
      padding: 0.75rem 1rem;
      border-radius: var(--radius-md);
      font-size: 0.85rem;
      color: var(--text-secondary);
      display: flex;
      gap: 0.5rem;
    }

    .positions-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1.25rem;
    }

    .position-card {
      padding: 1.25rem;
      border-radius: var(--radius-lg);
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .position-card.low {
      border: 1px solid var(--warning);
    }

    .card-top {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .provider-badge {
      font-size: 0.75rem;
      font-weight: 700;
      color: var(--text-secondary);
      background-color: var(--bg-surface);
      padding: 3px 8px;
      border-radius: 4px;
    }

    .status-pill {
      font-size: 0.65rem;
      font-weight: 700;
      padding: 2px 7px;
      border-radius: 4px;
    }

    .status-pill.healthy { background: var(--success-bg); color: var(--success); }
    .status-pill.surplus { background: var(--info-bg); color: var(--info); }
    .status-pill.low { background: var(--warning-bg); color: var(--warning); }

    .account-title {
      font-size: 1.05rem;
      font-weight: 700;
    }

    .account-num {
      font-size: 0.75rem;
      font-family: monospace;
      color: var(--text-muted);
    }

    .balance-display {
      display: flex;
      align-items: baseline;
      gap: 0.4rem;
      margin-top: 0.4rem;
    }

    .balance-display .currency {
      font-size: 0.85rem;
      font-weight: 700;
      color: var(--safari-gold);
    }

    .balance-display .amount {
      font-size: 1.8rem;
      font-weight: 800;
    }

    .progress-section {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
      margin-top: 0.5rem;
    }

    .progress-labels {
      display: flex;
      justify-content: space-between;
      font-size: 0.7rem;
      color: var(--text-muted);
    }

    .progress-track {
      height: 6px;
      background-color: var(--border);
      border-radius: 3px;
      overflow: hidden;
    }

    .progress-fill {
      height: 100%;
      background-color: var(--primary);
      border-radius: 3px;
    }

    .card-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-top: 0.5rem;
      padding-top: 0.75rem;
      border-top: 1px solid rgba(255, 255, 255, 0.05);
    }

    .timestamp {
      font-size: 0.7rem;
      color: var(--text-muted);
    }

    .btn-action {
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--primary);
    }
  `]
})
export class LiquidityComponent implements OnInit {
  private apiService = inject(ApiService);
  tenantService = inject(TenantService);

  liquidity = signal<LiquiditySummary | null>(null);

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.apiService.getLiquiditySummary().subscribe(res => this.liquidity.set(res));
  }

  calcPercentage(val: number, max: number): number {
    if (!max || max <= 0) return 100;
    return Math.min(100, Math.round((val / max) * 100));
  }

  simulateRebalance(): void {
    alert('Rebalance order dispatched: KES 150,000 sweep scheduled from Equity Bank to Airtel Money Float.');
  }
}
