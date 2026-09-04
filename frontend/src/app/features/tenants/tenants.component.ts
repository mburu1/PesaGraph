import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TenantService } from '../../core/services/tenant.service';

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="tenants-view animate-fade">
      <div class="page-header">
        <div>
          <h1>Tenant Management & Provider Rail Adapters</h1>
          <p class="subtitle">Multi-tenant isolation and credentials status for Kenyan payment rails</p>
        </div>
        <button (click)="openNewTenantModal()" class="btn-primary">+ Onboard New Tenant</button>
      </div>

      <!-- Tenants Grid -->
      <div class="tenants-grid">
        @for (t of tenantService.tenants(); track t.id) {
          <div class="tenant-card glass-panel" [class.selected]="t.id === tenantService.currentTenant().id">
            <div class="card-header">
              <div>
                <h3>{{ t.name }}</h3>
                <div class="slug-id">{{ t.slug }} &bull; {{ t.id }}</div>
              </div>
              @if (t.id === tenantService.currentTenant().id) {
                <span class="active-tag">CURRENT ACTIVE</span>
              }
            </div>

            <!-- Provider Integrations Status -->
            <div class="rails-status">
              <h4>Configured Payment & Messaging Rails</h4>
              <div class="rail-row">
                <div class="rail-name">
                  <span class="dot active"></span>
                  <strong>Safaricom Daraja 3.0</strong>
                </div>
                <span class="rail-detail">Paybill 600120 & Till 102938</span>
              </div>

              <div class="rail-row">
                <div class="rail-name">
                  <span class="dot active"></span>
                  <strong>Airtel Money API</strong>
                </div>
                <span class="rail-detail">Agent Float AIR-992180</span>
              </div>

              <div class="rail-row">
                <div class="rail-name">
                  <span class="dot active"></span>
                  <strong>Meta WhatsApp Cloud API</strong>
                </div>
                <span class="rail-detail">+254 700 000 000 (Webhook live)</span>
              </div>

              <div class="rail-row">
                <div class="rail-name">
                  <span class="dot active"></span>
                  <strong>Bank Statement Feeds</strong>
                </div>
                <span class="rail-detail">Equity Bank, KCB Bank</span>
              </div>
            </div>

            <div class="card-footer">
              <span class="created-at">Created {{ t.createdAtUtc | date:'mediumDate' }}</span>
              <button (click)="tenantService.selectTenant(t.id)" class="btn-switch">
                {{ t.id === tenantService.currentTenant().id ? 'Selected' : 'Switch Context' }}
              </button>
            </div>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .tenants-view {
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

    .tenants-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(360px, 1fr));
      gap: 1.5rem;
    }

    .tenant-card {
      padding: 1.5rem;
      border-radius: var(--radius-lg);
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    .tenant-card.selected {
      border: 2px solid var(--primary);
      box-shadow: 0 0 15px var(--primary-glow);
    }

    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
    }

    .card-header h3 {
      font-size: 1.15rem;
      font-weight: 700;
    }

    .slug-id {
      font-size: 0.75rem;
      color: var(--text-muted);
      font-family: monospace;
      margin-top: 0.2rem;
    }

    .active-tag {
      background-color: var(--primary-light);
      color: var(--primary);
      font-size: 0.7rem;
      font-weight: 700;
      padding: 2px 8px;
      border-radius: 4px;
    }

    .rails-status {
      background: var(--bg-surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-md);
      padding: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.6rem;
    }

    .rails-status h4 {
      font-size: 0.8rem;
      color: var(--text-muted);
      font-weight: 700;
      letter-spacing: 0.05em;
    }

    .rail-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.8rem;
    }

    .rail-name {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background-color: var(--text-muted);
    }

    .dot.active {
      background-color: var(--success);
      box-shadow: 0 0 6px var(--success);
    }

    .rail-detail {
      color: var(--text-secondary);
      font-size: 0.75rem;
    }

    .card-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-top: auto;
      padding-top: 0.75rem;
      border-top: 1px solid rgba(255, 255, 255, 0.05);
    }

    .created-at {
      font-size: 0.75rem;
      color: var(--text-muted);
    }

    .btn-switch {
      background-color: var(--bg-surface);
      border: 1px solid var(--border);
      color: var(--text-primary);
      font-size: 0.8rem;
      font-weight: 600;
      padding: 0.4rem 0.85rem;
      border-radius: var(--radius-md);
    }

    .btn-switch:hover {
      background-color: var(--primary);
      color: white;
      border-color: var(--primary);
    }
  `]
})
export class TenantsComponent {
  tenantService = inject(TenantService);

  openNewTenantModal(): void {
    const name = prompt('Enter Tenant / SACCO Name:');
    if (name) {
      const slug = name.toLowerCase().replace(/[^a-z0-9]/g, '-');
      this.tenantService.addTenant(name, slug);
    }
  }
}
