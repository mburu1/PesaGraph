import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { TenantService } from './core/services/tenant.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="app-layout">
      <!-- Sidebar -->
      <aside class="sidebar">
        <div class="brand">
          <div class="brand-logo">
            <span class="logo-circle">PG</span>
          </div>
          <div class="brand-text">
            <h2>PesaGraph</h2>
            <span class="badge-rail">KENYA-OPS</span>
          </div>
        </div>

        <!-- Tenant Selector -->
        <div class="tenant-selector-box">
          <label>ACTIVE TENANT</label>
          <select [value]="tenantService.currentTenant().id" (change)="onTenantChange($event)">
            @for (t of tenantService.tenants(); track t.id) {
              <option [value]="t.id">{{ t.name }}</option>
            }
          </select>
        </div>

        <nav class="nav-links">
          <a routerLink="/dashboard" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">📊</span>
            <span>Dashboard</span>
          </a>
          <a routerLink="/liquidity" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">💰</span>
            <span>Liquidity Cockpit</span>
          </a>
          <a routerLink="/reconciliation" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">⚖️</span>
            <span>Reconciliation</span>
          </a>
          <a routerLink="/exceptions" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">⚠️</span>
            <span>Exceptions Queue</span>
            <span class="counter-badge">21</span>
          </a>
          <a routerLink="/conversational" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">💬</span>
            <span>WhatsApp Bot</span>
            <span class="live-dot"></span>
          </a>
          <a routerLink="/tenants" routerLinkActive="active" class="nav-item">
            <span class="nav-icon">🏢</span>
            <span>Tenancy & Rails</span>
          </a>
        </nav>

        <div class="sidebar-footer">
          <div class="provider-rail-status">
            <div class="rail-item"><span class="status-indicator active"></span> Daraja 3.0</div>
            <div class="rail-item"><span class="status-indicator active"></span> Airtel Money</div>
            <div class="rail-item"><span class="status-indicator active"></span> Bank Feeds</div>
          </div>
          <div class="user-profile">
            <div class="avatar">OM</div>
            <div class="info">
              <span class="name">Operations Lead</span>
              <span class="role">Finance & Treasury</span>
            </div>
          </div>
        </div>
      </aside>

      <!-- Main Container -->
      <main class="main-content">
        <!-- Top Navbar -->
        <header class="topbar">
          <div class="breadcrumb">
            <span class="org-name">{{ tenantService.currentTenant().name }}</span>
            <span class="divider">/</span>
            <span class="env-tag">LIVE RAILS</span>
          </div>

          <div class="topbar-actions">
            <div class="quick-status">
              <span class="currency-tag">KES</span>
              <span class="time-label">EAT (UTC+3)</span>
            </div>
            <a routerLink="/conversational" class="btn-whatsapp-action">
              <span>💬 Test WhatsApp Flow</span>
            </a>
          </div>
        </header>

        <!-- Dynamic Feature Content -->
        <div class="content-body">
          <router-outlet></router-outlet>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .app-layout {
      display: flex;
      height: 100vh;
      overflow: hidden;
      background-color: var(--bg-dark);
    }

    .sidebar {
      width: 270px;
      background-color: var(--bg-surface);
      border-right: 1px solid var(--border);
      display: flex;
      flex-direction: column;
      padding: 1.25rem 1rem;
      gap: 1.25rem;
    }

    .brand {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding-bottom: 0.5rem;
      border-bottom: 1px solid var(--border);
    }

    .logo-circle {
      width: 40px;
      height: 40px;
      border-radius: var(--radius-md);
      background: linear-gradient(135deg, var(--primary) 0%, #047857 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 800;
      color: white;
      font-size: 1.1rem;
      box-shadow: 0 4px 12px var(--primary-glow);
    }

    .brand-text h2 {
      font-size: 1.2rem;
      font-weight: 700;
      letter-spacing: -0.02em;
    }

    .badge-rail {
      font-size: 0.65rem;
      font-weight: 700;
      letter-spacing: 0.05em;
      color: var(--primary);
      background: var(--primary-light);
      padding: 2px 6px;
      border-radius: 4px;
    }

    .tenant-selector-box {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
    }

    .tenant-selector-box label {
      font-size: 0.65rem;
      letter-spacing: 0.06em;
      font-weight: 700;
      color: var(--text-muted);
    }

    .tenant-selector-box select {
      width: 100%;
      font-size: 0.85rem;
      background-color: var(--bg-card);
      border: 1px solid var(--border);
      color: var(--text-primary);
    }

    .nav-links {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
      flex: 1;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.65rem 0.85rem;
      border-radius: var(--radius-md);
      font-size: 0.9rem;
      font-weight: 500;
      color: var(--text-secondary);
      transition: all 0.2s ease;
      position: relative;
    }

    .nav-item:hover {
      background-color: var(--bg-card);
      color: var(--text-primary);
    }

    .nav-item.active {
      background-color: var(--primary-glow);
      color: var(--primary);
      font-weight: 600;
    }

    .counter-badge {
      margin-left: auto;
      background-color: var(--danger);
      color: white;
      font-size: 0.7rem;
      font-weight: 700;
      padding: 2px 7px;
      border-radius: 999px;
    }

    .live-dot {
      margin-left: auto;
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background-color: var(--success);
      box-shadow: 0 0 8px var(--success);
    }

    .sidebar-footer {
      border-top: 1px solid var(--border);
      padding-top: 1rem;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .provider-rail-status {
      display: flex;
      flex-direction: column;
      gap: 0.3rem;
      font-size: 0.75rem;
      color: var(--text-muted);
    }

    .rail-item {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .status-indicator {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background-color: var(--text-muted);
    }

    .status-indicator.active {
      background-color: var(--success);
    }

    .user-profile {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .avatar {
      width: 34px;
      height: 34px;
      border-radius: 50%;
      background-color: var(--border);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.8rem;
      font-weight: 700;
      color: var(--text-primary);
    }

    .info {
      display: flex;
      flex-direction: column;
    }

    .info .name {
      font-size: 0.85rem;
      font-weight: 600;
    }

    .info .role {
      font-size: 0.7rem;
      color: var(--text-muted);
    }

    .main-content {
      flex: 1;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    .topbar {
      height: 64px;
      background-color: var(--bg-surface);
      border-bottom: 1px solid var(--border);
      padding: 0 1.5rem;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .breadcrumb {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      font-size: 0.9rem;
    }

    .org-name {
      font-weight: 600;
    }

    .divider {
      color: var(--text-muted);
    }

    .env-tag {
      font-size: 0.7rem;
      font-weight: 700;
      color: var(--success);
      background-color: var(--success-bg);
      padding: 3px 8px;
      border-radius: 4px;
    }

    .topbar-actions {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .quick-status {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.8rem;
      color: var(--text-secondary);
    }

    .currency-tag {
      font-weight: 700;
      color: var(--safari-gold);
    }

    .btn-whatsapp-action {
      background-color: #25d366;
      color: #075e54;
      font-weight: 700;
      font-size: 0.85rem;
      padding: 0.5rem 1rem;
      border-radius: var(--radius-md);
      display: flex;
      align-items: center;
      gap: 0.4rem;
      transition: all 0.2s ease;
    }

    .btn-whatsapp-action:hover {
      background-color: #20ba59;
      transform: translateY(-1px);
    }

    .content-body {
      flex: 1;
      overflow-y: auto;
      padding: 1.5rem;
    }
  `]
})
export class AppComponent {
  tenantService = inject(TenantService);

  onTenantChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.tenantService.selectTenant(select.value);
  }
}
