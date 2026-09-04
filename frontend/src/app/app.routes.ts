import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'liquidity',
    loadComponent: () => import('./features/liquidity/liquidity.component').then(m => m.LiquidityComponent)
  },
  {
    path: 'reconciliation',
    loadComponent: () => import('./features/reconciliation/reconciliation.component').then(m => m.ReconciliationComponent)
  },
  {
    path: 'exceptions',
    loadComponent: () => import('./features/exceptions/exceptions.component').then(m => m.ExceptionsComponent)
  },
  {
    path: 'conversational',
    loadComponent: () => import('./features/conversational/conversational.component').then(m => m.ConversationalComponent)
  },
  {
    path: 'tenants',
    loadComponent: () => import('./features/tenants/tenants.component').then(m => m.TenantsComponent)
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
