# PesaGraph Architecture & Design Diagrams Index

This directory provides complete architectural documentation and diagrammatic models for the **PesaGraph** platform. All diagrams are authored in standard [Mermaid.js](https://mermaid.js.org/) syntax, making them render natively on GitHub, IDE markdown viewers, and documentation tools.

## Diagram Catalogue

| Document | Type | Contents & Bounded Contexts |
| :--- | :--- | :--- |
| **[System Architecture](file:///d:/Mwangi%20Wa%20Mburu/Coding/PesaGraph/docs/architecture/system-architecture.md)** | C4 Context, Container & Components | C4 Level 1 (System Context with Daraja, Airtel, Banks & Meta), C4 Level 2 (SPA, API, Workers, PostgreSQL, Redis, Mongo, RabbitMQ), C4 Level 3 (Modular Monolith package decoupling) |
| **[Class Diagrams & OOAD](file:///d:/Mwangi%20Wa%20Mburu/Coding/PesaGraph/docs/architecture/class-diagrams.md)** | UML Class / Domain Model | Domain aggregates (`Tenant`, `CanonicalTransaction`, `Account`, `JournalEntry`, `ReconciliationSession`, `AuditLogEntry`), Value objects (`Money`, `Currency`), Strategy patterns for Providers & Matching rules |
| **[Sequence Diagrams](file:///d:/Mwangi%20Wa%20Mburu/Coding/PesaGraph/docs/architecture/sequence-diagrams.md)** | UML Sequence Lifelines | 1. Real-time Webhook Ingestion & Canonical Normalisation<br/>2. Cross-Rail Automated Reconciliation Run<br/>3. WhatsApp Conversational Interface Execution<br/>4. Discrepancy Resolution & Tamper-Evident Audit Chaining |
| **[State Machine Diagrams](file:///d:/Mwangi%20Wa%20Mburu/Coding/PesaGraph/docs/architecture/state-diagrams.md)** | UML Statecharts | `CanonicalTransaction` state lifecycle, `ReconciliationSession` execution states, Float health transitions, and WhatsApp session state machine |
| **[Database ERD](file:///d:/Mwangi%20Wa%20Mburu/Coding/PesaGraph/docs/architecture/database-erd.md)** | Entity-Relationship Diagram | Relational schema mapped in PostgreSQL EF Core (`Tenants`, `ApiKeys`, `CanonicalTransactions`, `Accounts`, `JournalEntries`, `Postings`, `ReconciliationSessions`, `AuditLogs`) |
