import { Injectable, signal } from '@angular/core';
import { Tenant } from '../models/models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TenantService {
  private readonly defaultTenants: Tenant[] = [
    {
      id: environment.defaultTenantId,
      name: 'PesaConnect SACCO Ltd',
      slug: 'pesaconnect-sacco',
      status: 'Active',
      createdAtUtc: '2026-01-15T08:00:00Z',
      currency: 'KES'
    },
    {
      id: '00000000-0000-0000-0000-000000000002',
      name: 'Nairobi Super-Agent Retailers',
      slug: 'nairobi-retailers',
      status: 'Active',
      createdAtUtc: '2026-02-01T09:30:00Z',
      currency: 'KES'
    },
    {
      id: '00000000-0000-0000-0000-000000000003',
      name: 'AfriPay Micro-Finance',
      slug: 'afripay-mfi',
      status: 'Active',
      createdAtUtc: '2026-03-10T11:20:00Z',
      currency: 'KES'
    }
  ];

  tenants = signal<Tenant[]>(this.defaultTenants);
  currentTenant = signal<Tenant>(this.defaultTenants[0]);

  selectTenant(tenantId: string): void {
    const found = this.tenants().find(t => t.id === tenantId);
    if (found) {
      this.currentTenant.set(found);
    }
  }

  addTenant(name: string, slug: string): Tenant {
    const newTenant: Tenant = {
      id: crypto.randomUUID(),
      name,
      slug,
      status: 'Active',
      createdAtUtc: new Date().toISOString(),
      currency: 'KES'
    };
    this.tenants.update(list => [...list, newTenant]);
    return newTenant;
  }
}
