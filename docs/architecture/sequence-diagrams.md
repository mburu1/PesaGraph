# PesaGraph End-to-End Sequence Diagrams

This document illustrates the interactions, message passes, and lifecycle lifelines for the four critical workflows in PesaGraph.

## 1. Webhook Ingestion & Canonical Normalisation Sequence

```mermaid
sequenceDiagram
    autonumber
    actor Customer as Payer / Customer
    participant Daraja as Safaricom Daraja 3.0
    participant WebhookCtrl as IngestionWebhookController
    participant Normalizer as MpesaNormaliser
    participant Mongo as MongoDb (RawArchive)
    participant Bus as MassTransit RabbitMQ Bus
    participant Consumer as CanonicalTransactionConsumer
    participant Ledger as LedgerService
    participant Postgres as PostgreSQL (ApplicationDbContext)
    participant Redis as Redis Cache (FloatTracker)

    Customer->>Daraja: Pays via Paybill/Till (STK Push or C2B)
    Daraja->>WebhookCtrl: POST /api/v1/ingestionwebhook/mpesa (JSON Payload)
    
    activate WebhookCtrl
    WebhookCtrl->>Mongo: InsertRawPayloadAsync(tenantId, "Daraja", rawJson)
    WebhookCtrl->>Normalizer: NormaliseAsync(darajaPayload)
    Normalizer-->>WebhookCtrl: CanonicalTransaction (Normalized KES, Reference, Status)
    
    WebhookCtrl->>Postgres: Save CanonicalTransaction (Status: Received)
    WebhookCtrl->>Bus: Publish(CanonicalTransactionCreatedEvent)
    WebhookCtrl-->>Daraja: 200 OK ({"ResultCode": 0, "ResultDesc": "Accepted"})
    deactivate WebhookCtrl

    Note over Bus,Consumer: Asynchronous Event Processing
    Bus->>Consumer: Consume(CanonicalTransactionCreatedEvent)
    activate Consumer
    Consumer->>Ledger: PostCanonicalTransactionAsync(canonicalTx)
    activate Ledger
    Ledger->>Postgres: Insert JournalEntry & Postings (Debit Cash / Credit Clearing)
    Ledger->>Postgres: Update Account Balances
    Ledger-->>Consumer: JournalEntryCreated
    deactivate Ledger

    Consumer->>Redis: Invalidate & Update Live Float Balance
    Consumer->>Postgres: Update CanonicalTransaction (Status: Normalised)
    deactivate Consumer
```

---

## 2. Cross-Rail Automated Reconciliation Run Sequence

```mermaid
sequenceDiagram
    autonumber
    actor Operator as Finance Lead (SPA / Cron)
    participant ReconCtrl as ReconciliationController
    participant ReconSvc as ReconciliationService
    participant RuleEngine as ReconciliationRuleEngine
    participant Postgres as PostgreSQL (Transactions & Sessions)
    participant Audit as AuditService
    participant Redis as Redis (Distributed Lock)

    Operator->>ReconCtrl: POST /api/v1/reconciliation/run
    ReconCtrl->>ReconSvc: StartReconciliationSessionAsync(tenantId, dateWindow)
    
    activate ReconSvc
    ReconSvc->>Redis: AcquireLockAsync("recon-lock:{tenantId}", 5min)
    alt Lock Failed
        ReconSvc-->>ReconCtrl: 409 Conflict ("Session already running")
    else Lock Acquired
        ReconSvc->>Postgres: Create ReconciliationSession (Status: Running)
        ReconSvc->>Postgres: Fetch Unmatched CanonicalTransactions(tenantId, dateWindow)
        
        loop For Each Provider Transaction
            ReconSvc->>RuleEngine: EvaluateRules(candidateTx, candidatePool)
            activate RuleEngine
            RuleEngine->>RuleEngine: 1. Try ExactReferenceRule
            RuleEngine->>RuleEngine: 2. Try FuzzyInvoiceHeuristic
            RuleEngine->>RuleEngine: 3. Try FeeToleranceDeltaRule
            RuleEngine-->>ReconSvc: MatchResult (Matched / Discrepancy / Unmatched)
            deactivate RuleEngine

            alt Matched
                ReconSvc->>Postgres: Create MatchedPair(leftId, rightId, score)
                ReconSvc->>Postgres: Mark Transactions (Status: Reconciled)
            else Discrepancy Found
                ReconSvc->>Postgres: Create DiscrepancyItem(reason, deltaAmount)
            end
        end

        ReconSvc->>Postgres: Finalize Session (Status: Completed, MatchRate: 98.5%)
        ReconSvc->>Audit: RecordAuditChainAsync("ReconciliationRunCompleted", sessionId)
        ReconSvc->>Redis: ReleaseLockAsync("recon-lock:{tenantId}")
        ReconSvc-->>ReconCtrl: ReconciliationSummaryResponse
        deactivate ReconSvc
    end

    ReconCtrl-->>Operator: 200 OK (Summary Data)
```

---

## 3. WhatsApp Conversational Interface Execution Sequence

```mermaid
sequenceDiagram
    autonumber
    actor Operator as Operator (Mobile WhatsApp)
    participant MetaCloud as Meta WhatsApp Cloud API
    participant BotCtrl as ConversationalWebhookController
    participant CommandParser as ConversationalCommandService
    participant Liquidity as LiquidityService
    participant Recon as ReconciliationService
    participant WhatsAppClient as WhatsAppClient (Outbound)
    participant Audit as AuditService

    Note over MetaCloud,BotCtrl: Step A: Handshake (One-time)
    MetaCloud->>BotCtrl: GET /api/v1/conversationalwebhook/whatsapp?hub.mode=subscribe&hub.challenge=123
    BotCtrl-->>MetaCloud: 200 OK (Challenge: 123)

    Note over Operator,BotCtrl: Step B: Operator Command Flow
    Operator->>MetaCloud: Sends message: "float"
    MetaCloud->>BotCtrl: POST /api/v1/conversationalwebhook/whatsapp (Inbound Payload)
    
    activate BotCtrl
    BotCtrl->>CommandParser: ProcessInboundMessageAsync(fromPhone, "float")
    activate CommandParser
    
    CommandParser->>Liquidity: GetConsolidatedFloatSummaryAsync(tenantId)
    Liquidity-->>CommandParser: FloatSummary (M-Pesa, Airtel, Banks, Total)
    
    CommandParser->>CommandParser: FormatKenyaOperationsCard(floatSummary)
    CommandParser->>WhatsAppClient: SendTextMessageAsync(fromPhone, formattedMessage)
    activate WhatsAppClient
    WhatsAppClient->>MetaCloud: POST /v20.0/{phoneId}/messages (JSON)
    WhatsAppClient-->>CommandParser: MessageQueued
    deactivate WhatsAppClient

    CommandParser->>Audit: LogConversationalInteractionAsync(operatorPhone, "float", response)
    CommandParser-->>BotCtrl: Result.Success
    deactivate CommandParser

    BotCtrl-->>MetaCloud: 200 OK
    deactivate BotCtrl

    MetaCloud-->>Operator: Delivers WhatsApp Message with Float Card
```

---

## 4. Discrepancy Resolution & Audit Hash Chaining Sequence

```mermaid
sequenceDiagram
    autonumber
    actor FinanceUser as Finance Lead (Frontend SPA)
    participant Api as ExceptionsController
    participant ReconSvc as ReconciliationService
    participant LedgerSvc as LedgerService
    participant AuditSvc as AuditService
    participant Postgres as PostgreSQL (ApplicationDbContext)

    FinanceUser->>Api: POST /api/v1/reconciliation/exceptions/{id}/resolve (Notes)
    activate Api
    Api->>ReconSvc: ResolveDiscrepancyAsync(exceptionId, notes, userId)
    
    activate ReconSvc
    ReconSvc->>Postgres: Fetch DiscrepancyItem & UnmatchedTransaction
    ReconSvc->>LedgerSvc: PostAdjustingEntryAsync(varianceAmount, "Tariff adjustment")
    LedgerSvc->>Postgres: Insert JournalEntry (Adjustment Posting)
    
    ReconSvc->>Postgres: Update Discrepancy (Status: ResolvedByOperator)
    ReconSvc->>Postgres: Update CanonicalTransaction (Status: Reconciled)
    
    ReconSvc->>AuditSvc: RecordAuditChainEntryAsync(tenantId, action, oldVal, newVal)
    activate AuditSvc
    AuditSvc->>Postgres: Fetch Latest AuditLogEntry (PreviousHash)
    AuditSvc->>AuditSvc: Compute SHA-256(PreviousHash + EntityId + Action + Timestamp)
    AuditSvc->>Postgres: Insert New AuditLogEntry with Chain Hash
    AuditSvc-->>ReconSvc: AuditRecorded
    deactivate AuditSvc

    ReconSvc-->>Api: ResolutionSuccess
    deactivate ReconSvc
    Api-->>FinanceUser: 200 OK (Item Resolved)
    deactivate Api
```
