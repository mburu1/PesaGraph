# PesaGraph Relational Data Model (Entity-Relationship Diagram)

This document maps out the relational database schema implemented with Entity Framework Core (PostgreSQL) in `PesaGraph.Infrastructure.Persistence.ApplicationDbContext`.

## Entity-Relationship Diagram (ERD)

```mermaid
erDiagram
    TENANTS ||--o{ API_KEYS : "owns"
    TENANTS ||--o{ CANONICAL_TRANSACTIONS : "isolates"
    TENANTS ||--o{ ACCOUNTS : "manages"
    TENANTS ||--o{ JOURNAL_ENTRIES : "records"
    TENANTS ||--o{ RECONCILIATION_SESSIONS : "executes"
    TENANTS ||--o{ AUDIT_LOG_ENTRIES : "audits"

    ACCOUNTS ||--o{ POSTINGS : "balances"
    JOURNAL_ENTRIES ||--|{ POSTINGS : "contains"

    RECONCILIATION_SESSIONS ||--o{ MATCHED_PAIRS : "contains"
    RECONCILIATION_SESSIONS ||--o{ DISCREPANCY_ITEMS : "flags"

    CANONICAL_TRANSACTIONS ||--o{ MATCHED_PAIRS : "left/right"
    CANONICAL_TRANSACTIONS ||--o{ DISCREPANCY_ITEMS : "references"

    TENANTS {
        uuid id PK
        varchar name
        varchar slug UK
        varchar status
        varchar currency
        timestamptz created_at_utc
    }

    API_KEYS {
        uuid id PK
        uuid tenant_id FK
        varchar key_hash
        varchar key_prefix
        varchar description
        boolean is_revoked
        timestamptz expires_at_utc
        timestamptz created_at_utc
    }

    CANONICAL_TRANSACTIONS {
        uuid id PK
        uuid tenant_id FK
        varchar provider
        varchar channel
        varchar provider_reference
        varchar external_reference
        numeric amount
        varchar currency
        varchar type
        varchar status
        varchar source_account
        varchar destination_account
        timestamptz occurred_at_utc
        jsonb metadata
    }

    ACCOUNTS {
        uuid id PK
        uuid tenant_id FK
        varchar account_number UK
        varchar name
        varchar type
        varchar currency
        numeric current_balance
        boolean is_active
        timestamptz updated_at_utc
    }

    JOURNAL_ENTRIES {
        uuid id PK
        uuid tenant_id FK
        varchar reference_number UK
        timestamptz posted_at_utc
        text description
    }

    POSTINGS {
        uuid id PK
        uuid journal_entry_id FK
        uuid account_id FK
        numeric debit
        numeric credit
    }

    RECONCILIATION_SESSIONS {
        uuid id PK
        uuid tenant_id FK
        varchar status
        timestamptz started_at_utc
        timestamptz completed_at_utc
        int total_processed
        int matched_count
        int unmatched_count
        int discrepancy_count
        numeric match_rate_percentage
    }

    MATCHED_PAIRS {
        uuid id PK
        uuid session_id FK
        uuid left_transaction_id FK
        uuid right_transaction_id FK
        varchar rule_applied
        numeric confidence_score
        timestamptz matched_at_utc
    }

    DISCREPANCY_ITEMS {
        uuid id PK
        uuid session_id FK
        uuid transaction_id FK
        varchar reason
        numeric variance_amount
        varchar status
        text resolution_notes
        timestamptz created_at_utc
    }

    AUDIT_LOG_ENTRIES {
        uuid id PK
        uuid tenant_id FK
        varchar action
        varchar entity_name
        varchar entity_id
        varchar performed_by
        text old_values_json
        text new_values_json
        varchar previous_hash
        varchar current_hash
        timestamptz timestamp_utc
    }
```
