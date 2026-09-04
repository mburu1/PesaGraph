export interface Result<T> {
  isSuccess: boolean;
  isFailure: boolean;
  value?: T;
  error?: {
    code: string;
    message: string;
    type: number;
  };
}

export interface Tenant {
  id: string;
  name: string;
  slug: string;
  status: string;
  createdAtUtc: string;
  currency: string;
}

export interface CanonicalTransaction {
  id: string;
  tenantId: string;
  provider: 'M-Pesa' | 'AirtelMoney' | 'Bank' | 'Cash';
  channel: string;
  providerReference: string;
  externalReference?: string;
  amount: number;
  currency: string;
  type: 'Inflow' | 'Outflow';
  sourceAccount?: string;
  destinationAccount?: string;
  status: 'Received' | 'Normalised' | 'Reconciled' | 'Disputed' | 'Failed';
  occurredAtUtc: string;
  metadata?: Record<string, any>;
}

export interface AccountBalance {
  id: string;
  tenantId: string;
  accountNumber: string;
  name: string;
  type: 'Asset' | 'Liability' | 'Equity' | 'Revenue' | 'Expense';
  currency: string;
  balance: number;
  provider?: string;
  lastUpdatedUtc?: string;
}

export interface FloatPosition {
  provider: string;
  accountName: string;
  accountNumber: string;
  currentBalance: number;
  currency: string;
  minimumThreshold: number;
  optimalThreshold: number;
  status: 'Healthy' | 'Low' | 'Critical' | 'Surplus';
  lastUpdatedUtc: string;
}

export interface LiquiditySummary {
  tenantId: string;
  totalLiquidity: number;
  currency: string;
  positions: FloatPosition[];
  lowFloatCount: number;
  criticalFloatCount: number;
  recommendations: string[];
}

export interface ReconciliationSummary {
  sessionId: string;
  tenantId: string;
  status: 'Running' | 'Completed' | 'Failed';
  startedAtUtc: string;
  completedAtUtc?: string;
  totalProcessed: number;
  matchedCount: number;
  unmatchedCount: number;
  discrepancyCount: number;
  matchRatePercentage: number;
  unmatchedVolume: number;
  currency: string;
}

export interface UnmatchedTransaction {
  id: string;
  tenantId: string;
  provider: string;
  reference: string;
  amount: number;
  currency: string;
  type: 'Inflow' | 'Outflow';
  counterparty: string;
  occurredAtUtc: string;
  discrepancyReason?: string;
  suggestedMatch?: {
    candidateId: string;
    candidateRef: string;
    confidenceScore: number;
    amountDifference: number;
  };
}

export interface WhatsAppMessage {
  id: string;
  sender: 'user' | 'agent';
  text: string;
  timestamp: string;
  metadata?: any;
}
