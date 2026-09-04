# PesaGraph Architecture & System Context (C4 Context & Container)

This document maps out the system context, containers, and external payment and messaging rails that PesaGraph interacts with.

## 1. System Context Diagram (C4 Level 1)

```mermaid
C4Context
    title System Context Diagram for PesaGraph Platform

    Person(operator, "Business Operator / Finance Manager", "Manages reconciliation, float monitoring, and approves exceptions via Web or WhatsApp.")
    Person(customer, "Payer / Customer", "Makes payments via M-Pesa Till/Paybill, Airtel, or Bank.")

    System(pesagraph, "PesaGraph Platform", "Multi-rail payment reconciliation, canonical ledger, float liquidity intelligence, and conversational assistant.")

    System_Ext(daraja, "Safaricom Daraja 3.0", "Processes M-Pesa C2B, B2C, STK Push, and Statement Webhooks.")
    System_Ext(airtel, "Airtel Money API", "Processes collections, disbursements, and float balance queries.")
    System_Ext(banks, "Commercial Bank Feeds", "Provides account statement files and API feeds (Equity, KCB, Absa, Co-op).")
    System_Ext(whatsapp, "Meta WhatsApp Cloud API", "Two-way conversational channel for operator commands and alerts.")
    System_Ext(sms, "SMS Gateway (Africa's Talking)", "Fallback alerts, digests, and multi-factor notifications.")

    Rel(customer, daraja, "Pays via M-Pesa STK / Paybill")
    Rel(customer, airtel, "Pays via Airtel Till")
    Rel(customer, banks, "Transfers to Bank Account")

    Rel(daraja, pesagraph, "Sends transaction webhooks & balance reports", "HTTPS/JSON")
    Rel(airtel, pesagraph, "Sends payment callbacks & float updates", "HTTPS/JSON")
    Rel(banks, pesagraph, "Streams statement feeds / files", "HTTPS/SFTP")

    Rel(operator, pesagraph, "Monitors liquidity, resolves exceptions", "Angular SPA (HTTPS)")
    Rel(operator, whatsapp, "Sends 'float', 'unmatched', 'resolve'", "WhatsApp Chat")
    Rel(whatsapp, pesagraph, "Delivers two-way conversational webhooks", "HTTPS/JSON")
    Rel(pesagraph, whatsapp, "Replies with formatted operational cards", "HTTPS/JSON")
    Rel(pesagraph, sms, "Dispatches critical low-float and audit SMS alerts", "HTTPS/JSON")
```

---

## 2. Container Diagram (C4 Level 2)

```mermaid
C4Container
    title Container Diagram for PesaGraph

    Person(operator, "Operator / Finance Lead", "Manages daily operations and reconciliation.")

    Container_Boundary(c1, "PesaGraph Platform") {
        Container(spa, "Single-Page App (SPA)", "Angular 19, TypeScript, CSS", "Provides liquidity cockpit, reconciliation matrix, and exception resolution dashboard.")
        Container(api, "API Gateway / Modular Monolith", "ASP.NET Core 10, C#", "Exposes REST endpoints, validates tenant API keys, ingests multi-rail webhooks.")
        Container(workers, "Background Workers", ".NET 10 Worker Service, MassTransit", "Asynchronously processes canonical transactions, periodic reconciliation jobs, and outbox event dispatching.")
        
        ContainerDb(postgres, "Relational Database", "PostgreSQL 16", "Stores tenants, api keys, canonical transactions, double-entry ledger accounts, and audit log hash chains.")
        ContainerDb(redis, "In-Memory Cache & Lock Store", "Redis 7", "Stores live float positions, distributed locks, rate-limiting tokens, and WhatsApp session state.")
        ContainerDb(mongo, "Raw Event & Payload Store", "MongoDB 7", "Immutable store of raw provider JSON payloads for replay and forensic auditing.")
        ContainerDb(rabbitmq, "Message Broker", "RabbitMQ / MassTransit", "Decouples webhook ingestion from ledger posting, reconciliation runs, and notifications.")
    }

    System_Ext(daraja, "Safaricom Daraja API", "M-Pesa Webhooks")
    System_Ext(airtel, "Airtel Money API", "Airtel Webhooks")
    System_Ext(whatsapp, "Meta WhatsApp Cloud API", "Conversational Handshake")

    Rel(operator, spa, "Uses", "HTTPS")
    Rel(spa, api, "Makes API calls", "JSON/HTTPS")
    Rel(daraja, api, "Posts C2B/B2C webhooks", "JSON/HTTPS")
    Rel(airtel, api, "Posts payment webhooks", "JSON/HTTPS")
    Rel(whatsapp, api, "Sends message webhooks", "JSON/HTTPS")

    Rel(api, rabbitmq, "Publishes CanonicalTransactionCreatedEvent", "AMQP")
    Rel(rabbitmq, workers, "Consumes events", "AMQP")
    Rel(api, postgres, "Reads/Writes ledger & tenant records", "EF Core")
    Rel(api, mongo, "Archives raw provider payloads", "MongoDB Driver")
    Rel(api, redis, "Caches float & session state", "StackExchange.Redis")

    Rel(workers, postgres, "Executes batch reconciliation & ledger updates", "EF Core")
    Rel(workers, redis, "Acquires distributed session locks", "StackExchange.Redis")
```

---

## 3. Modular Monolith Component Architecture (C4 Level 3)

```mermaid
graph TD
    subgraph PesaGraph.Api
        API[API Host & Controllers]
        MW[Tenant Resolution & Auth Middleware]
    end

    subgraph Domain Modules [Bounded Contexts]
        Tenancy[Tenancy Module]
        Providers[Provider Adapters]
        Ingestion[Ingestion & Normalisation]
        Ledger[Double-Entry Ledger]
        Recon[Reconciliation Engine]
        Liquidity[Liquidity & Float Intelligence]
        Conversational[Conversational Bot Context]
        Notifications[Notifications & Outbox]
        Audit[Tamper-Evident Audit Chain]
    end

    subgraph Infrastructure Layer
        EF[ApplicationDbContext / PostgreSQL]
        MongoRepo[Mongo Raw Payload Archive]
        RedisCache[Redis Cache & Distributed Locks]
        Bus[MassTransit RabbitMQ Bus]
    end

    API --> MW
    MW --> Tenancy
    API --> Providers
    API --> Ingestion
    API --> Ledger
    API --> Recon
    API --> Liquidity
    API --> Conversational

    Providers -->|Raw Event| Ingestion
    Ingestion -->|CanonicalTransactionCreated| Bus
    Bus -->|Consume| Ingestion
    Ingestion -->|Post Journal Entry| Ledger
    Ledger -->|Update Float Balance| Liquidity
    Recon -->|Compare| Ledger
    Conversational -->|Query Float / Resolve| Liquidity
    Conversational -->|Resolve Exception| Recon
    Notifications -->|Alert Dispatches| Providers
    Ledger -->|Hash Chain Entry| Audit
    Recon -->|Session Audit| Audit

    Tenancy --> EF
    Ledger --> EF
    Recon --> EF
    Audit --> EF
    Ingestion --> MongoRepo
    Liquidity --> RedisCache
    Conversational --> RedisCache
```
