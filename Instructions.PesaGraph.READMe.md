# PesaGraph — Multi-Rail Payment Reconciliation & Liquidity Intelligence Platform

**Kenya-first. Consumption-only. Operations brain for M-Pesa, Airtel Money, Banks, SMS & WhatsApp.**

This document is the single source of truth for the product vision, scope, architecture decisions, and repository structure. It is deliberately free of technology-stack sales pitches. It exists so any senior engineer (or recruiter) can understand *what* is being built, *why*, and *how* the system is organised before a single line of production code is written.

---

## 1. Problem Statement (Kenya Reality)

Kenyan SMEs, agents, super-agents, SACCOs and small fintechs almost never operate on a single rail.

Typical daily reality:
- M-Pesa Till / Paybill (Daraja)
- Airtel Money
- One or more bank accounts (Equity, KCB, Absa, Co-op, NCBA, etc.)
- Cash float
- SMS confirmations and WhatsApp screenshots flying around

Consequences:
- Finance teams spend 1–4 hours every day matching transactions across different statement formats, reference styles, fees and settlement timings.
- Float surprises (one wallet is dry while another is over-funded).
- Customer disputes that can only be resolved by asking for screenshots.
- Missed revenue, delayed month-end close, and zero real-time visibility.

Existing open-source projects either wrap a single provider or try to become a full ERP. Almost none focus on the *intelligence and reconciliation layer* that sits on top of the rails the business already uses.

PesaGraph is that layer.

---

## 2. Core Idea

PesaGraph is a multi-tenant platform that **only consumes** existing Kenyan payment and messaging APIs. It never becomes a payment processor or a wallet itself.

It:
1. Ingests real-time webhooks, statements, balance queries and message events from multiple rails.
2. Normalises everything into a single canonical ledger.
3. Automatically reconciles across providers using configurable matching rules + fuzzy logic.
4. Surfaces real-time float / liquidity across all wallets and bank accounts.
5. Detects anomalies and low-float situations.
6. Lets operators and business owners interact via **WhatsApp** (and SMS) exactly the way KRA allows tax interactions without visiting a website.

The WhatsApp channel is a first-class citizen, not an afterthought. A business owner or agent should be able to:

- Send “float” → receive current balances across M-Pesa, Airtel and bank accounts.
- Send “unmatched” → receive the list of items that still need attention.
- Send “resolve REF12345” → mark an item as reconciled and optionally notify the customer.
- Receive proactive daily digests and low-float alerts.
- Initiate a reconciliation run or request a summary for a date range.

This mirrors the conversational pattern Kenyans already trust with KRA, banks and utilities.

---

## 3. Scope — What PesaGraph Is and Is Not

### Is
- Multi-rail ingestion & normalisation engine
- Intelligent cross-provider reconciliation engine
- Real-time liquidity / float cockpit
- Conversational interface (WhatsApp Business API + SMS)
- Multi-tenant SaaS-style platform with strong isolation
- Exception queue + audit trail for finance teams
- Event-driven core that can later feed analytics or ML matching

### Is Not
- A payment gateway or processor
- A replacement for Daraja / Airtel / bank systems
- A full accounting package or ERP
- A consumer-facing wallet app
- A project that invents new payment rails

---

## 4. Primary External Systems Consumed

| System                  | Purpose in PesaGraph                                      | Interaction Style          |
|-------------------------|-----------------------------------------------------------|----------------------------|
| Safaricom Daraja 3.0    | M-Pesa STK, C2B, B2C, status, balance, statements         | Webhooks + polling         |
| Airtel Money API        | Collections, disbursements, balance, statements           | Webhooks + polling         |
| Bank APIs / Feeds       | Account statements, balances (Equity, KCB, Absa, etc.)    | Webhooks / file / polling  |
| WhatsApp Business API   | Two-way conversational interface + proactive alerts       | Cloud API + webhooks       |
| SMS Gateway             | Fallback notifications, two-way confirmations, digests    | API + delivery reports     |

All credentials are stored per tenant and never leave the platform’s secure boundary.

---

## 5. Architecture Recommendation

### Recommended Starting Point: Modular Monolith

**Why not microservices from day one?**
- A portfolio / small-team project that tries to be microservices too early pays a massive operational tax (distributed transactions, service discovery, network latency, deployment complexity, observability sprawl).
- The domain has clear bounded contexts that can live comfortably inside one deployable unit while remaining extractable later.
- Recruiters and hiring managers value clean modular design far more than premature distribution.

**Bounded Contexts (logical modules inside the monolith):**
- Identity & Tenancy
- Provider Adapters (Daraja, Airtel, Banks, WhatsApp, SMS)
- Ingestion & Normalisation
- Ledger
- Reconciliation Engine
- Liquidity & Forecasting
- Conversational Interface (WhatsApp / SMS command handling)
- Notifications & Digests
- Audit & Compliance
- Administration & Reporting

Each context communicates with others primarily through in-process interfaces or domain events. When scale or team size justifies it, any context can be extracted into its own service with minimal rewrite because the boundaries are already explicit.

### Messaging Strategy

Both RabbitMQ and Kafka are supported in the repository layout so the team can choose (or run both for different purposes).

**Primary recommendation: RabbitMQ + MassTransit (or native .NET clients)**
- Excellent for command / event patterns inside a modular monolith.
- Mature .NET ecosystem, easy local development, strong support for retries, delayed messages, sagas and outbox pattern.
- Perfect for webhook fan-out, reconciliation jobs, notification commands and conversational command handling.

**Kafka (or Redpanda) as optional secondary**
- Use when you need a durable, high-throughput, replayable event log of every normalised transaction for analytics, machine-learning matching experiments, or future audit requirements.
- Keep the operational path on RabbitMQ; stream a copy of canonical events into Kafka.

The repository contains configuration and Docker Compose profiles for both so the choice is explicit and reversible.

---

## 6. High-Level Data Strategy

- **Relational store** — tenants, users, matching rules, matched pairs, reconciliation runs, configuration, audit trail.
- **Document / event store** — raw provider payloads, normalised transaction events, conversational session state, high-volume logs.
- **Cache / coordination store (Redis)** — live balances, rate-limit tokens, short-lived reconciliation locks, real-time dashboard projections, WhatsApp session context.

This combination is deliberate: relational for consistency and reporting, document for volume and schema flexibility, Redis for speed and coordination.

---

## 7. Repository Structure (Tree)

```
pesagraph/
├── .github/
│   └── workflows/                  # CI pipelines (build, test, lint, docker)
├── docs/
│   ├── architecture/               # C4 diagrams, ADRs, sequence diagrams
│   ├── domain/                     # Ubiquitous language, reconciliation rules examples
│   └── operations/                 # Runbooks, WhatsApp command reference
├── src/
│   ├── PesaGraph.Api/              # ASP.NET entry point (HTTP + webhooks)
│   ├── PesaGraph.Workers/          # Background processors, reconciliation jobs
│   ├── PesaGraph.Modules/
│   │   ├── Tenancy/
│   │   ├── Providers/              # Daraja, Airtel, Bank, WhatsApp, SMS adapters
│   │   ├── Ingestion/
│   │   ├── Ledger/
│   │   ├── Reconciliation/
│   │   ├── Liquidity/
│   │   ├── Conversational/         # WhatsApp + SMS command handlers
│   │   ├── Notifications/
│   │   └── Audit/
│   ├── PesaGraph.Shared/           # Kernel, common abstractions, result types
│   └── PesaGraph.Infrastructure/   # Persistence, messaging, external clients
├── tests/
│   ├── PesaGraph.UnitTests/
│   ├── PesaGraph.IntegrationTests/ # Provider contract tests with fixtures
│   └── PesaGraph.ArchitectureTests/
├── frontend/                       # Angular application
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/
│   │   │   ├── shared/
│   │   │   ├── features/
│   │   │   │   ├── dashboard/
│   │   │   │   ├── reconciliation/
│   │   │   │   ├── liquidity/
│   │   │   │   ├── exceptions/
│   │   │   │   ├── tenants/
│   │   │   │   └── conversational/ # WhatsApp session preview / admin
│   │   └── environments/
│   └── angular.json
├── docker/
│   ├── docker-compose.yml          # Full local stack
│   ├── docker-compose.rabbitmq.yml
│   ├── docker-compose.kafka.yml
│   ├── Dockerfile.api
│   ├── Dockerfile.workers
│   └── Dockerfile.frontend
├── scripts/                        # Local setup, seed data, migration helpers
├── .env.example
├── Directory.Build.props
├── PesaGraph.sln
└── README.md                       # Short pointer to this Instructions document
```

---

## 8. Docker & Local Development

The `docker/` folder provides a complete local environment:

- API + Workers
- Relational database
- Document store
- Redis
- RabbitMQ (management UI)
- Kafka + Zookeeper / Redpanda (optional profile)
- Angular frontend (or volume-mounted for hot reload)
- Mock / sandbox credential injection points for Daraja, Airtel and WhatsApp

One command brings the entire stack up. Profiles allow developers to run “rabbitmq-only”, “kafka-only”, or “both” depending on the feature they are working on.

---

## 9. Messaging Layout (Both Technologies Supported)

```
Infrastructure/
├── Messaging/
│   ├── RabbitMq/
│   │   ├── MassTransit configuration
│   │   ├── Consumers (webhook events, reconciliation commands, WhatsApp commands)
│   │   └── Outbox / Inbox implementation
│   └── Kafka/
│       ├── Producers (canonical transaction events)
│       ├── Consumers (analytics, future ML pipeline)
│       └── Topic naming conventions
```

Canonical domain events are published once. RabbitMQ handles the operational path; Kafka (when enabled) receives a durable copy for replay and analytics.

---

## 10. WhatsApp Integration Principles

- Treat WhatsApp as a first-class conversational channel, not a notification bolt-on.
- All inbound messages are turned into commands or queries against the domain (float, unmatched, resolve, summary, help).
- Session context is short-lived and stored in Redis.
- Outbound messages are rate-limited and respect Meta’s messaging windows and templates where required.
- Every conversation is audited.
- The same command handlers can be reused by SMS for users who prefer or only have SMS.

This design allows a business owner to manage reconciliation and liquidity from the same WhatsApp thread they already use for customer conversations — exactly the KRA-style experience requested.

---

## 11. Success Criteria for the First Vertical Slice

A recruiter or technical interviewer should be able to:

1. Spin up the stack with Docker.
2. Configure sandbox credentials for Daraja + one bank + WhatsApp + SMS.
3. See incoming transactions appear in the Angular exception queue in near real time.
4. Trigger a reconciliation run and watch matches appear.
5. Send a WhatsApp message “float” and receive current balances.
6. Resolve an unmatched item from WhatsApp and see the ledger update.
7. Inspect clean module boundaries and event flow in the code.

Everything beyond this vertical slice is incremental and should not block the demonstration of senior engineering judgement.

---

## 12. Guiding Principles (Non-Negotiable)

- Consumption only — never become a payment rail.
- Modular monolith first, extract later.
- Explicit boundaries over clever abstractions.
- Idempotency and auditability on every external interaction.
- WhatsApp and SMS are product features, not afterthoughts.
- The repository must be understandable by a senior engineer in under 30 minutes.
- Every major decision is recorded as an Architecture Decision Record (ADR).

---

## 13. Next Steps After Cloning

1. Read this document fully.
2. Review `docs/architecture/` for the current C4 and sequence diagrams.
3. Copy `.env.example` → `.env` and fill sandbox credentials.
4. `docker compose -f docker/docker-compose.yml up`.
5. Follow the “First Vertical Slice” checklist in `docs/operations/`.

This Instructions document is the living product and architecture contract. Update it whenever the vision or major structural decisions change.
