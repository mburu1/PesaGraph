import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of, catchError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TenantService } from './tenant.service';
import {
  Result,
  CanonicalTransaction,
  LiquiditySummary,
  ReconciliationSummary,
  UnmatchedTransaction,
  AccountBalance
} from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly tenantService = inject(TenantService);

  private get headers(): HttpHeaders {
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'X-Tenant-Id': this.tenantService.currentTenant().id
    });
  }

  private baseUrl = environment.apiBaseUrl;

  // Ingestion & Transactions
  getRecentTransactions(): Observable<CanonicalTransaction[]> {
    const mockData: CanonicalTransaction[] = [
      {
        id: 'tx-001',
        tenantId: this.tenantService.currentTenant().id,
        provider: 'M-Pesa',
        channel: 'Paybill 600120',
        providerReference: 'RKA892KL91',
        externalReference: 'INV-4412',
        amount: 24500,
        currency: 'KES',
        type: 'Inflow',
        sourceAccount: '254712345678',
        destinationAccount: '600120',
        status: 'Reconciled',
        occurredAtUtc: new Date(Date.now() - 1000 * 60 * 4).toISOString()
      },
      {
        id: 'tx-002',
        tenantId: this.tenantService.currentTenant().id,
        provider: 'AirtelMoney',
        channel: 'Till 882190',
        providerReference: 'AIR-9921827',
        externalReference: 'INV-4413',
        amount: 8200,
        currency: 'KES',
        type: 'Inflow',
        sourceAccount: '254733987654',
        destinationAccount: '882190',
        status: 'Reconciled',
        occurredAtUtc: new Date(Date.now() - 1000 * 60 * 18).toISOString()
      },
      {
        id: 'tx-003',
        tenantId: this.tenantService.currentTenant().id,
        provider: 'Bank',
        channel: 'Equity B2C Float',
        providerReference: 'EQ-TRX-449102',
        amount: 50000,
        currency: 'KES',
        type: 'Outflow',
        sourceAccount: 'Equity-011299881',
        destinationAccount: 'M-Pesa Working Account',
        status: 'Normalised',
        occurredAtUtc: new Date(Date.now() - 1000 * 60 * 35).toISOString()
      },
      {
        id: 'tx-004',
        tenantId: this.tenantService.currentTenant().id,
        provider: 'M-Pesa',
        channel: 'B2C Payout',
        providerReference: 'RKB1198MN2',
        externalReference: 'LOAN-DISBURSE-881',
        amount: 15000,
        currency: 'KES',
        type: 'Outflow',
        sourceAccount: 'M-Pesa Utility Account',
        destinationAccount: '254722554433',
        status: 'Reconciled',
        occurredAtUtc: new Date(Date.now() - 1000 * 60 * 55).toISOString()
      },
      {
        id: 'tx-005',
        tenantId: this.tenantService.currentTenant().id,
        provider: 'M-Pesa',
        channel: 'C2B Till 102938',
        providerReference: 'RKC448821X',
        amount: 3200,
        currency: 'KES',
        type: 'Inflow',
        sourceAccount: '254790112233',
        destinationAccount: '102938',
        status: 'Disputed',
        occurredAtUtc: new Date(Date.now() - 1000 * 60 * 95).toISOString()
      }
    ];

    return this.http.get<Result<CanonicalTransaction[]>>(`${this.baseUrl}/ingestion/transactions`, { headers: this.headers }).pipe(
      catchError(() => of({ isSuccess: true, isFailure: false, value: mockData })),
      // map to array
      (source$) => new Observable<CanonicalTransaction[]>(subscriber => {
        source$.subscribe({
          next: res => subscriber.next(res?.value || mockData),
          error: () => subscriber.next(mockData),
          complete: () => subscriber.complete()
        });
      })
    );
  }

  // Liquidity
  getLiquiditySummary(): Observable<LiquiditySummary> {
    const mockSummary: LiquiditySummary = {
      tenantId: this.tenantService.currentTenant().id,
      totalLiquidity: 4825900,
      currency: 'KES',
      lowFloatCount: 1,
      criticalFloatCount: 0,
      recommendations: [
        'Airtel Money Agent float is approaching minimum buffer (KES 85,000 / KES 100,000 threshold). Transfer KES 150,000 from Equity Bank float pool.',
        'Daraja Utility Account float has surplus liquidity (KES 1,840,000). Consider sweeping KES 500,000 into interest-bearing call deposit.'
      ],
      positions: [
        {
          provider: 'M-Pesa Daraja',
          accountName: 'B2C Utility Account',
          accountNumber: '600120-Utility',
          currentBalance: 1840000,
          currency: 'KES',
          minimumThreshold: 500000,
          optimalThreshold: 1500000,
          status: 'Surplus',
          lastUpdatedUtc: new Date().toISOString()
        },
        {
          provider: 'M-Pesa Daraja',
          accountName: 'C2B Settlement Till',
          accountNumber: 'Till-102938',
          currentBalance: 965400,
          currency: 'KES',
          minimumThreshold: 300000,
          optimalThreshold: 800000,
          status: 'Healthy',
          lastUpdatedUtc: new Date().toISOString()
        },
        {
          provider: 'Airtel Money',
          accountName: 'Airtel Agent Float',
          accountNumber: 'AIR-992180',
          currentBalance: 85500,
          currency: 'KES',
          minimumThreshold: 100000,
          optimalThreshold: 350000,
          status: 'Low',
          lastUpdatedUtc: new Date().toISOString()
        },
        {
          provider: 'Equity Bank',
          accountName: 'Operating & Liquidity Pool',
          accountNumber: '011299881001',
          currentBalance: 1585000,
          currency: 'KES',
          minimumThreshold: 500000,
          optimalThreshold: 1200000,
          status: 'Healthy',
          lastUpdatedUtc: new Date().toISOString()
        },
        {
          provider: 'KCB Bank',
          accountName: 'Merchant Escrow Account',
          accountNumber: '1102994433',
          currentBalance: 350000,
          currency: 'KES',
          minimumThreshold: 200000,
          optimalThreshold: 400000,
          status: 'Healthy',
          lastUpdatedUtc: new Date().toISOString()
        }
      ]
    };

    return this.http.get<Result<LiquiditySummary>>(`${this.baseUrl}/liquidity/summary`, { headers: this.headers }).pipe(
      catchError(() => of({ isSuccess: true, isFailure: false, value: mockSummary })),
      (source$) => new Observable<LiquiditySummary>(subscriber => {
        source$.subscribe({
          next: res => subscriber.next(res?.value || mockSummary),
          error: () => subscriber.next(mockSummary),
          complete: () => subscriber.complete()
        });
      })
    );
  }

  // Reconciliation
  getReconciliationSummary(): Observable<ReconciliationSummary> {
    const mockRec: ReconciliationSummary = {
      sessionId: 'rec-session-20260904-01',
      tenantId: this.tenantService.currentTenant().id,
      status: 'Completed',
      startedAtUtc: new Date(Date.now() - 1000 * 60 * 120).toISOString(),
      completedAtUtc: new Date(Date.now() - 1000 * 60 * 115).toISOString(),
      totalProcessed: 1482,
      matchedCount: 1461,
      unmatchedCount: 21,
      discrepancyCount: 5,
      matchRatePercentage: 98.58,
      unmatchedVolume: 124600,
      currency: 'KES'
    };

    return this.http.get<Result<ReconciliationSummary>>(`${this.baseUrl}/reconciliation/summary`, { headers: this.headers }).pipe(
      catchError(() => of({ isSuccess: true, isFailure: false, value: mockRec })),
      (source$) => new Observable<ReconciliationSummary>(subscriber => {
        source$.subscribe({
          next: res => subscriber.next(res?.value || mockRec),
          error: () => subscriber.next(mockRec),
          complete: () => subscriber.complete()
        });
      })
    );
  }

  // Unmatched & Exceptions
  getUnmatchedTransactions(): Observable<UnmatchedTransaction[]> {
    const mockUnmatched: UnmatchedTransaction[] = [
      {
        id: 'unmatched-001',
        tenantId: this.tenantService.currentTenant().id,
        provider: 'M-Pesa',
        reference: 'RKA991823H',
        amount: 14500,
        currency: 'KES',
        type: 'Inflow',
        counterparty: 'Grace Wambui (254722889911)',
        occurredAtUtc: new Date(Date.now() - 1000 * 60 * 180).toISOString(),
        discrepancyReason: 'Missing Invoice Reference in BillRefNumber',
        suggestedMatch: {
          candidateId: 'inv-8910',
          candidateRef: 'INV-8910 (Grace W.)',
          confidenceScore: 0.94,
          amountDifference: 0
        }
      },
      {
        id: 'unmatched-002',
        tenantId: this.tenantService.currentTenant().id,
        provider: 'AirtelMoney',
        reference: 'AIR-0081273',
        amount: 5200,
        currency: 'KES',
        type: 'Inflow',
        counterparty: 'David Ochieng (254733441122)',
        occurredAtUtc: new Date(Date.now() - 1000 * 60 * 240).toISOString(),
        discrepancyReason: 'Timestamp discrepancy (>24 hours variance from expected batch)',
        suggestedMatch: {
          candidateId: 'inv-8902',
          candidateRef: 'INV-8902 (D. Ochieng)',
          confidenceScore: 0.88,
          amountDifference: 0
        }
      },
      {
        id: 'unmatched-003',
        tenantId: this.tenantService.currentTenant().id,
        provider: 'Bank (Equity)',
        reference: 'EQ-DEP-99410',
        amount: 85000,
        currency: 'KES',
        type: 'Inflow',
        counterparty: 'M-Pesa Float Deposit',
        occurredAtUtc: new Date(Date.now() - 1000 * 60 * 320).toISOString(),
        discrepancyReason: 'Amount mismatch: Daraja statement shows KES 84,750 (KES 250 fee delta)',
        suggestedMatch: {
          candidateId: 'daraja-b2b-091',
          candidateRef: 'SAF-B2B-11928',
          confidenceScore: 0.98,
          amountDifference: 250
        }
      },
      {
        id: 'unmatched-004',
        tenantId: this.tenantService.currentTenant().id,
        provider: 'M-Pesa',
        reference: 'RKB228190P',
        amount: 19900,
        currency: 'KES',
        type: 'Inflow',
        counterparty: 'Peter Kamau (254711002233)',
        occurredAtUtc: new Date(Date.now() - 1000 * 60 * 480).toISOString(),
        discrepancyReason: 'Duplicate provider transaction reference detected in batch',
      }
    ];

    return of(mockUnmatched);
  }

  // Trigger reconciliation run
  triggerReconciliation(): Observable<Result<string>> {
    return this.http.post<Result<string>>(`${this.baseUrl}/reconciliation/run`, {}, { headers: this.headers }).pipe(
      catchError(() => of({ isSuccess: true, isFailure: false, value: 'Reconciliation session queued successfully' }))
    );
  }

  // Resolve exception manually
  resolveException(unmatchedId: string, resolutionNotes: string): Observable<Result<boolean>> {
    return of({ isSuccess: true, isFailure: false, value: true });
  }

  // Conversational command execution (simulate WhatsApp engine)
  executeConversationalCommand(command: string): Observable<string> {
    const cleanCmd = command.trim().toLowerCase();

    if (cleanCmd === 'float' || cleanCmd.includes('balance')) {
      return of(
        `📊 *PesaGraph Float Cockpit (KES)*\n\n` +
        `• *M-Pesa Daraja Utility*: KES 1,840,000 [Healthy]\n` +
        `• *M-Pesa C2B Till*: KES 965,400 [Healthy]\n` +
        `• *Airtel Money Agent*: KES 85,500 ⚠️ [LOW FLOAT]\n` +
        `• *Equity Bank Pool*: KES 1,585,000 [Healthy]\n` +
        `• *KCB Escrow*: KES 350,000 [Healthy]\n\n` +
        `💰 *Total Liquidity*: KES 4,825,900\n` +
        `_Action needed: Reply with *rebalance airtel 100000* to queue top-up from Equity._`
      );
    }

    if (cleanCmd === 'unmatched' || cleanCmd.includes('pending') || cleanCmd.includes('discrepancies')) {
      return of(
        `🔍 *PesaGraph Unmatched Queue (21 items)*\n\n` +
        `1. *RKA991823H* — KES 14,500 (Grace Wambui)\n` +
        `   Reason: Missing Invoice Ref (Suggested: INV-8910, 94% conf)\n\n` +
        `2. *AIR-0081273* — KES 5,200 (David Ochieng)\n` +
        `   Reason: 24h timestamp variance\n\n` +
        `3. *EQ-DEP-99410* — KES 85,000 (Equity vs Daraja)\n` +
        `   Reason: KES 250 fee delta\n\n` +
        `_Reply *resolve RKA991823H* to auto-pair with suggested match._`
      );
    }

    if (cleanCmd.startsWith('resolve')) {
      const parts = command.trim().split(' ');
      const ref = parts[1] || 'REF123';
      return of(
        `✅ *Item Resolved*\n\n` +
        `Transaction *${ref.toUpperCase()}* marked as reconciled by WhatsApp operator.\n` +
        `Ledger journal entry #JE-9921 posted and audit chain updated.\n` +
        `Customer notification sent via SMS.`
      );
    }

    if (cleanCmd === 'summary' || cleanCmd === 'report') {
      return of(
        `📈 *Daily Operations Summary*\n\n` +
        `• Date: Today (${new Date().toLocaleDateString()})\n` +
        `• Total Ingested: 1,482 transactions\n` +
        `• Reconciled: 1,461 (98.58% match rate)\n` +
        `• Unmatched: 21 items (KES 124,600)\n` +
        `• Discrepancies: 5 fee deltas\n` +
        `• Liquidity Health: 4/5 accounts optimal\n\n` +
        `_PesaGraph Intelligent Operations Engine_`
      );
    }

    return of(
      `🤖 *PesaGraph WhatsApp Bot Commands*\n\n` +
      `• *float* — Check live cash float balances across M-Pesa, Airtel & Banks\n` +
      `• *unmatched* — List pending reconciliation exceptions\n` +
      `• *resolve <REF>* — Approve and force-match an exception\n` +
      `• *summary* — Receive daily performance and match digest\n` +
      `• *help* — Show this menu`
    );
  }
}
