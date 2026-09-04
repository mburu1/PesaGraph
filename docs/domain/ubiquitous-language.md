# PesaGraph — Ubiquitous Language Glossary

> This glossary is the **single source of truth** for domain terminology used across code, diagrams, conversations, and documentation. Every term defined here maps directly to a class, value object, or concept in the codebase. Use these terms consistently — in code identifiers, PR descriptions, Jira tickets, and verbal communication.

---

## Core Domain Terms

### Rail

A **payment channel or provider network** that PesaGraph connects to for ingestion. A rail is never managed by PesaGraph — it is always external.

| Rail | Provider | Typical Interaction |
| --- | --- | --- |
| `M-Pesa` | Safaricom Daraja 3.0 | STK Push, C2B, B2C, Balance, Statement |
| `AirtelMoney` | Airtel Money API | Collections, Disbursements, Balance |
| `Bank` | Equity / KCB / Absa / Co-op | Account Statement, Balance, Webhooks |
| `WhatsApp` | Meta Cloud API | Inbound commands, Outbound alerts |
| `SMS` | Africa's Talking / Infobip | Confirmations, Fallback digests |

---

### Tenant

A **business entity** (SME, agent, SACCO, fintech) that has an account on PesaGraph. All data is strictly isolated per tenant. A tenant owns one or more **rail configurations**, **reconciliation rules**, and **accounts**.

- **Slug** — URL-safe, human-readable identifier (e.g. `kimani-superstore`)
- **TenantStatus** — `Active` | `Suspended` | `Deactivated`
- **ApiKey** — HMAC-hashed credential pair used for webhook authentication and API access

---

### CanonicalTransaction

The **normalised, provider-agnostic representation of a payment event** after it has been ingested from any rail. All downstream processing (ledger posting, reconciliation, liquidity update) operates on canonical transactions, never on raw provider payloads.

| Field | Meaning |
| --- | --- |
| `Provider` | Source rail: `M-Pesa`, `AirtelMoney`, `Bank` |
| `Channel` | Interaction type: `STK`, `C2B`, `B2C`, `BankCredit` |
| `ProviderReference` | Unique ID from the provider (e.g. Daraja `TransactionID`) |
| `ExternalReference` | Business reference set by the merchant |
| `Amount` | KES value object (`Money`) |
| `TransactionType` | `Inflow` \| `Outflow` |
| `Status` | `Received` → `Normalised` → `Reconciled` \| `Disputed` |
| `OccurredAtUtc` | When the event happened at the provider (not when ingested) |

---

### RawProviderPayload

The **verbatim JSON/XML body** received from a provider webhook or API response. Stored in MongoDB immediately upon receipt, before any transformation. Never modified after storage — provides an immutable audit trail and allows re-processing if normalisation logic changes.

---

### Normaliser / Adapter

A **provider-specific component** that transforms a `RawProviderPayload` into a `CanonicalTransaction`. One normaliser per rail (e.g. `MpesaNormaliser`, `AirtelNormaliser`). Normalisers are stateless, pure functions.

---

### JournalEntry

A **double-entry bookkeeping record** created for every `CanonicalTransaction`. Each journal entry has at least two `Posting` lines (debit and credit) that always balance.

- **Debit posting** — increases asset or expense account
- **Credit posting** — increases liability, equity, or income account
- **EntryType** — `Inflow` | `Outflow` | `Fee` | `Adjustment`

---

### Account (Ledger)

A **logical ledger account** tied to a specific rail and tenant. Not a provider account — it is PesaGraph's internal projection of a wallet or bank account balance. Types: `MpesaFloat`, `AirtelFloat`, `BankAccount`, `ClearingLiability`, `RevenueRecognised`.

---

### ReconciliationRun

A **bounded execution of the matching engine** for a tenant over a time window. A run produces a set of `ReconciliationMatch` records and leaves unmatched items in the `ExceptionQueue`.

| Field | Meaning |
| --- | --- |
| `RunId` | Unique identifier for the run |
| `TenantId` | Owning tenant |
| `WindowStart / End` | UTC time range processed |
| `Status` | `Pending` → `Running` → `Completed` \| `Failed` |
| `MatchedCount` | Transactions successfully paired |
| `UnmatchedCount` | Items remaining in exception queue |
| `MatchRatePercentage` | `MatchedCount / TotalCount * 100` |

---

### ReconciliationMatch

A **confirmed pairing** of two `CanonicalTransaction` records — one from each side of a rail boundary (e.g. Daraja inflow vs. bank credit). Confidence is expressed as a numeric score (0.0–1.0).

- **MatchMethod** — `ExactReference` | `AmountDateFuzzy` | `ManualOverride`
- **ConfidenceScore** — 1.0 for exact, <1.0 for fuzzy or manual

---

### ExceptionQueue

The **holding area for unmatched or disputed transactions** that require human review. Items in the exception queue are `CanonicalTransaction` records with status `Disputed` and no accepted `ReconciliationMatch`.

- **Resolution** — can be done via the Angular SPA or via a WhatsApp `resolve` command
- **ResolutionNote** — free-text explanation written by the operator

---

### LiquidityPosition

A **point-in-time snapshot of a single account's balance** relative to its configured thresholds. Computed on every ledger posting and cached in Redis.

| Status | Meaning |
| --- | --- |
| `Healthy` | Balance ≥ OptimalThreshold |
| `Surplus` | Balance > OptimalThreshold × 1.5 |
| `Low` | MinimumThreshold ≤ Balance < OptimalThreshold |
| `Critical` | Balance < MinimumThreshold |

---

### LiquiditySnapshot

A **multi-account summary** (`List<LiquidityPosition>`) computed for a tenant at a point in time. The Dashboard's "Float Cockpit" always displays the latest snapshot.

---

### ConversationalSession

A **short-lived WhatsApp or SMS conversation context** stored in Redis. Keyed by `TenantId + PhoneNumber`. Expires after 10 minutes of inactivity. Enables multi-turn interactions (e.g. "resolve" command followed by a confirmation prompt).

---

### ConversationalCommand

A **parsed intent extracted from an inbound WhatsApp or SMS message**. Commands map 1:1 to domain operations.

| Command | Trigger phrase(s) | Domain operation |
| --- | --- | --- |
| `FloatQuery` | "float", "balance", "float leo" | `GetLiquiditySnapshotQuery` |
| `UnmatchedQuery` | "unmatched", "exceptions" | `GetExceptionQueueQuery` |
| `ResolveCommand` | "resolve REF12345" | `ResolveExceptionCommand` |
| `RunReconciliationCommand` | "reconcile", "run recon" | `TriggerReconciliationRunCommand` |
| `SummaryQuery` | "summary 2024-01-01 2024-01-31" | `GetPeriodSummaryQuery` |
| `HelpCommand` | "help", "?" | Returns command menu |

---

### DigestJob

A **scheduled background job** that sends proactive summaries to tenants via WhatsApp and/or SMS. Examples: daily float summary at 08:00 EAT, low-float alerts triggered by threshold breach.

---

### AuditEntry

An **immutable record of every significant operation** on the platform. Written by the `AuditService` and stored in MongoDB. Covers: tenant login, reconciliation run, manual resolution, API key creation, rail credential update, WhatsApp command received/sent.

---

## Transaction Status Lifecycle

```
Received → Normalised → Reconciled
                    ↘ Disputed (ExceptionQueue)
                              ↘ Resolved (ManualOverride)
```

---

## Matching Confidence Scale

| Score | Method | Meaning |
| --- | --- | --- |
| 1.00 | `ExactReference` | Provider reference matches exactly on both sides |
| 0.85–0.99 | `AmountDateFuzzy` | Same amount + within 24h window, refs differ |
| 0.50–0.84 | `FuzzyLogic` | Partial reference match or extended time window |
| N/A | `ManualOverride` | Operator confirmed match despite low score |

---

## Bounded Context Map

```
┌─────────────────────────────────────────────────────────┐
│                     PesaGraph Monolith                   │
│                                                          │
│  ┌──────────┐   ┌───────────┐   ┌──────────────────┐   │
│  │ Tenancy  │──▶│ Providers │──▶│   Ingestion &    │   │
│  │          │   │ (Adapters)│   │   Normalisation  │   │
│  └──────────┘   └───────────┘   └────────┬─────────┘   │
│                                           │              │
│                          ┌────────────────▼──────────┐  │
│                          │          Ledger            │  │
│                          └────────────┬──────────────┘  │
│                                       │                  │
│             ┌─────────────────────────┼──────────────┐  │
│             │                         │              │  │
│  ┌──────────▼──────┐   ┌─────────────▼──┐  ┌───────▼─┐ │
│  │ Reconciliation  │   │   Liquidity &  │  │  Audit  │ │
│  │     Engine      │   │   Forecasting  │  │         │ │
│  └──────────┬──────┘   └────────────────┘  └─────────┘ │
│             │                                            │
│  ┌──────────▼────────────────────────────────────────┐  │
│  │           Conversational (WhatsApp / SMS)          │  │
│  └───────────────────────────────────────────────────┘  │
│                                                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │         Notifications & Digest Jobs               │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

---

## What PesaGraph Is NOT

| Anti-Pattern Term | Why It Does Not Apply |
| --- | --- |
| Payment Gateway | PesaGraph never initiates or routes payments |
| Wallet | PesaGraph never holds funds |
| ERP / Accounting System | PesaGraph provides a reconciliation layer, not a full ledger of record |
| Rail Replacement | PesaGraph only consumes external rails; it never replaces them |
