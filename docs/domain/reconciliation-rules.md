# PesaGraph — Reconciliation Rules Reference

> This document specifies the **matching rules** used by the `ReconciliationRuleEngine` to pair `CanonicalTransaction` records across payment rails. Rules are evaluated in priority order — the first matching rule wins. All rules are configurable per tenant via the Tenancy admin UI.

---

## 1. Rule Evaluation Pipeline

```
CanonicalTransactions (unmatched, within window)
          │
          ▼
   ┌──────────────────────────────┐
   │   Rule 1: ExactReference     │  ← Priority 1 (Highest)
   └──────────────┬───────────────┘
                  │ No match
                  ▼
   ┌──────────────────────────────┐
   │   Rule 2: AmountDateFuzzy    │  ← Priority 2
   └──────────────┬───────────────┘
                  │ No match
                  ▼
   ┌──────────────────────────────┐
   │   Rule 3: FuzzyReference     │  ← Priority 3
   └──────────────┬───────────────┘
                  │ No match
                  ▼
   ┌──────────────────────────────┐
   │   Exception Queue            │  ← Requires manual review
   └──────────────────────────────┘
```

---

## 2. Rule Definitions

### Rule 1 — Exact Reference Match

**Confidence:** 1.00 | **Method:** `ExactReference`

**Criteria:**

- `ProviderReference` on Side A equals `ProviderReference` or `ExternalReference` on Side B (case-insensitive)
- Both transactions belong to the same `TenantId`
- Amount difference ≤ configured fee tolerance (default: KES 35 — covers M-Pesa transaction charges)
- `OccurredAtUtc` difference ≤ 48 hours (covers T+1 bank settlement)

**Examples:**

```
Daraja webhook:  ProviderRef = "QHX72KPAS1"  Amount = KES 5,000
Bank statement:  ExternalRef = "QHX72KPAS1"  Amount = KES 4,978   ← KES 22 fee deducted
→ MATCHED (ExactReference, confidence 1.00, fee delta = KES 22)
```

---

### Rule 2 — Amount + Date Fuzzy Match

**Confidence:** 0.85–0.97 | **Method:** `AmountDateFuzzy`

**Criteria:**

- Exact amount match (or within fee tolerance)
- `OccurredAtUtc` within a configurable window (default: 72 hours)
- Same `TransactionType` (`Inflow`/`Outflow`)
- Provider references do **not** match (otherwise Rule 1 would have fired)

**Confidence degradation:**

| Time delta | Confidence |
| --- | --- |
| 0–6 hours | 0.97 |
| 6–24 hours | 0.92 |
| 24–48 hours | 0.88 |
| 48–72 hours | 0.85 |

**Example:**

```
Daraja:  Amount = KES 12,500   Time = 2024-01-15 09:42 UTC
Bank:    Amount = KES 12,478   Time = 2024-01-16 07:10 UTC  ← 21h 28m later (T+1 settlement)
→ MATCHED (AmountDateFuzzy, confidence 0.92)
```

---

### Rule 3 — Fuzzy Reference Match

**Confidence:** 0.50–0.84 | **Method:** `FuzzyLogic`

**Criteria:**

- Jaro-Winkler similarity between `ProviderReference` strings ≥ 0.80
- Amount within ±5% of each other
- `OccurredAtUtc` within 7 days

**Use case:** Typos or truncated references in manual bank entries, SMS-relayed codes.

**Example:**

```
Daraja:   ExternalRef = "INV-2024-00187"
Bank:     ExternalRef = "INV2024-00187"   ← dash missing
→ MATCHED (FuzzyLogic, confidence 0.76)
```

---

## 3. Fee Tolerance Configuration

| Provider Pair | Default Tolerance | Reason |
| --- | --- | --- |
| M-Pesa → Bank | KES 35 | M-Pesa transaction charge |
| Airtel → Bank | KES 30 | Airtel Money charge |
| Bank → Bank | KES 0 | Bank transfers typically exact |
| Any → Any | Configurable | Tenant-level override |

---

## 4. Reconciliation Window Defaults

| Setting | Default | Description |
| --- | --- | --- |
| `DefaultWindowHours` | 72 | Hours of history scanned per run |
| `MinimumConfidenceThreshold` | 0.85 | Below this score, item goes to exceptions |
| `AutoAcceptThreshold` | 1.00 | Score ≥ this is accepted automatically |
| `ManualReviewThreshold` | 0.50–0.84 | Surfaced in UI for human confirmation |
| `MaxBatchSize` | 10,000 | Max transactions processed per run |

---

## 5. Settlement Timing by Provider

| Provider | Typical Settlement | Notes |
| --- | --- | --- |
| M-Pesa C2B | Real-time (T+0) | Webhook arrives within seconds |
| M-Pesa B2C | T+0 to T+1 | Disbursement confirmation may lag |
| Airtel Money | T+0 to T+1 | Depends on network load |
| Equity Bank | T+1 (EFT) | End-of-day batch |
| KCB Bank | T+0 (RTGS) or T+1 (EFT) | Depends on transfer type |
| Absa Bank | T+1 | Standard EFT |

---

## 6. Manual Override Workflow

When automatic rules fail or produce a score below `MinimumConfidenceThreshold`:

1. Item enters the **ExceptionQueue** with status `Disputed`
2. Operator sees it in the Angular SPA **Exceptions Queue** view
3. Operator can:
   - **Confirm a low-confidence match** → status becomes `Reconciled`, method = `ManualOverride`
   - **Write off** → marks item as `Written Off` with mandatory note
   - **Escalate** → flags for senior review, sends notification
4. Alternatively, operator sends `resolve REF12345` via WhatsApp → same outcome

All manual actions are written to the `AuditEntry` log with operator identity and timestamp.

---

## 7. Cross-Rail Reconciliation Matrix

| Inflow Rail | Matched Against | Common Scenario |
| --- | --- | --- |
| M-Pesa | Bank Statement | Paybill settlement to bank |
| M-Pesa | Airtel Money | Agent-to-agent transfer |
| Airtel Money | Bank Statement | Collections settlement |
| Bank (Debit) | M-Pesa (Outflow) | B2C disbursement reconciliation |
| Bank (Credit) | Internal Ledger | Bank interest, charges |
| SMS Confirmation | Any rail tx | Fallback when no webhook received |

---

## 8. Idempotency Rules

- Every `ReconciliationRun` has a unique `RunId`
- Re-running over the same window does **not** create duplicate matches — existing `ReconciliationMatch` records are detected and skipped
- If a transaction is already `Reconciled`, it is excluded from subsequent runs
- All rule evaluations are deterministic given the same input set

---

## 9. Tenant Configuration Schema

```json
{
  "reconciliationRules": {
    "defaultWindowHours": 72,
    "minimumConfidenceThreshold": 0.85,
    "autoAcceptThreshold": 1.00,
    "feeTolerance": {
      "mpesaToBank": 35.00,
      "airtelToBank": 30.00,
      "bankToBank": 0.00
    },
    "providerPairs": [
      { "from": "M-Pesa", "to": "Bank", "enabled": true },
      { "from": "AirtelMoney", "to": "Bank", "enabled": true },
      { "from": "M-Pesa", "to": "AirtelMoney", "enabled": false }
    ]
  }
}
```
