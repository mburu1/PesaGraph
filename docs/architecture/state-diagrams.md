# PesaGraph State Machine & Entity Lifecycle Diagrams

This document contains state transition diagrams mapping the lifecycle of transactions, reconciliation sessions, float alert states, and conversational sessions.

## 1. Canonical Transaction Lifecycle State Machine

```mermaid
stateDiagram-v2
    [*] --> Received : Webhook payload ingested & validated
    
    Received --> Normalised : Normalised to standard currency (KES), channel & schema
    Received --> Failed : Invalid signature / payload schema error

    Normalised --> Matched : Exact / Fuzzy matching rule passes in reconciliation session
    Normalised --> Disputed : Tariff delta, timestamp mismatch, or missing external reference detected

    Disputed --> Matched : Manual approval or adjusting entry posted by operator
    Disputed --> WrittenOff : Unresolvable fraudulent / abandoned item

    Matched --> Reconciled : Double-entry journal entry confirmed and posted to ledger

    Reconciled --> [*]
    Failed --> [*]
    WrittenOff --> [*]
```

---

## 2. Reconciliation Session State Machine

```mermaid
stateDiagram-v2
    [*] --> Queued : Scheduled cron or manual trigger request
    
    Queued --> Running : Distributed lock acquired in Redis, batch window locked
    Queued --> Aborted : Concurrent session lock active for tenant

    Running --> EvaluatingRules : Iterating transactions through rule chain
    
    EvaluatingRules --> EvaluatingRules : Rule 1 (Exact) -> Rule 2 (Fuzzy) -> Rule 3 (Fee Delta)
    
    EvaluatingRules --> Finalizing : All window transactions evaluated
    
    Finalizing --> Completed : Summary metrics computed, audit hash chain appended
    Finalizing --> Failed : Database transaction or broker failure

    Completed --> [*]
    Failed --> [*]
    Aborted --> [*]
```

---

## 3. Float & Liquidity Position Health State Machine

```mermaid
stateDiagram-v2
    [*] --> Healthy : Float balance >= OptimalThreshold
    
    Healthy --> Surplus : Balance > OptimalThreshold * 1.5 (Opportunity for sweep)
    Healthy --> Low : Balance drops below MinimumThreshold
    
    Surplus --> Healthy : Swept to interest call deposit or float pool
    
    Low --> Critical : Balance drops below 50% of MinimumThreshold
    Low --> Healthy : Float top-up / rebalancing transfer completes
    
    Critical --> Healthy : Emergency sweep from bank reserves executed
    Critical --> Depleted : Balance reaches 0 (Outflows halted)

    Depleted --> Healthy : Account refilled
```

---

## 4. WhatsApp Conversational Session State Machine

```mermaid
stateDiagram-v2
    [*] --> Idle : No active conversational context
    
    Idle --> Authenticated : Inbound message from verified operator phone number
    Idle --> Rejected : Unrecognized sender phone number

    Authenticated --> ProcessingCommand : Parsing command (float, unmatched, resolve, summary)
    
    ProcessingCommand --> Responding : Domain query executed & operations card rendered
    ProcessingCommand --> AwaitingConfirmation : Command requires step confirmation (e.g. resolve ref)

    AwaitingConfirmation --> ProcessingCommand : Operator confirms ("yes" / "approve")
    AwaitingConfirmation --> Idle : Session timeout (15 mins TTL in Redis)

    Responding --> Idle : WhatsApp message dispatched to Meta Cloud API
    Rejected --> [*]
```
