# PesaGraph — WhatsApp Command Reference

> This document is the **complete reference** for the conversational interface available via WhatsApp Business API and SMS fallback. Business owners, agents, and finance leads interact with PesaGraph through the same WhatsApp thread they already use for customers — no separate app required.
>
> This mirrors the conversational pattern Kenyans already trust with KRA, banks and NHIF.

---

## Channel Entry Points

| Channel | Entry Point | Auth |
| --- | --- | --- |
| WhatsApp | Tenant's registered WhatsApp number | Session bound to phone number |
| SMS | Tenant's registered shortcode / sender | Phone number verified against tenant |

---

## Session Model

- Sessions are **keyed by `TenantId + PhoneNumber`** and stored in Redis
- Session TTL: **10 minutes** of inactivity
- Multi-turn commands (e.g. confirmation prompts) maintain state across messages
- First message from an unregistered number returns an onboarding reply

---

## Command Reference

### `float` — Current Balance Across All Rails

**Trigger phrases:** `float`, `balance`, `bakaa`, `float leo`, `show balance`

**Response:**

```
💰 *PesaGraph Float Report*
Tenant: Kimani Superstore
Time: 04 Sep 2024, 11:42 EAT

📱 M-Pesa Till 174379
   KES 87,450  ✅ HEALTHY

📱 Airtel Money
   KES 12,200  ⚠️ LOW (min: KES 15,000)

🏦 Equity Bank 0011234567
   KES 245,800  ✅ HEALTHY

🏦 KCB 1234567890
   KES 34,100  ✅ HEALTHY

─────────────────────
Total Float: KES 379,550

⚠️ Airtel Money is below minimum threshold.
```

---

### `unmatched` — Exception Queue Summary

**Trigger phrases:** `unmatched`, `exceptions`, `pending`, `unreconciled`

**Response:**

```
⚠️ *Unmatched Transactions*
Tenant: Kimani Superstore
As of: 04 Sep 2024, 11:42 EAT

21 items pending review
Total value: KES 127,450

Top 5 unmatched:
1. QHX72KP  KES 5,000  Daraja C2B  03 Sep
2. BA9KPLM  KES 12,500  Airtel Coll.  02 Sep
3. KES7290  KES 2,800  Daraja C2B  02 Sep
4. PLM44AB  KES 50,000  Bank RTGS  01 Sep
5. ZZR2214  KES 3,150  Daraja C2B  01 Sep

Reply: resolve <REF> to reconcile
       or visit https://app.pesagraph.co.ke/exceptions
```

---

### `resolve <REF>` — Resolve an Exception

**Trigger phrases:** `resolve QHX72KP`, `reconcile QHX72KP`, `mark resolved QHX72KP`

**Flow:**

```
User:  resolve QHX72KP

Bot:   ✅ Found unmatched transaction:
       Ref: QHX72KP
       Amount: KES 5,000
       Date: 03 Sep 2024
       Provider: Daraja C2B
       Status: Disputed

       Confirm resolution? Reply YES to confirm or NO to cancel.

User:  YES

Bot:   ✅ Transaction QHX72KP marked as resolved.
       Ledger updated. Exception queue: 20 remaining.
       Resolved by: +254 700 000 001 at 11:44 EAT
```

---

### `reconcile` — Trigger a Reconciliation Run

**Trigger phrases:** `reconcile`, `run recon`, `run reconciliation`, `match transactions`

**Response (immediate acknowledgement):**

```
⚙️ Reconciliation run started.
Tenant: Kimani Superstore
Window: Last 72 hours
Run ID: RUN-2024090411-A3F

You will receive a summary when complete (usually < 2 minutes).
```

**Response (on completion):**

```
✅ *Reconciliation Complete*
Run ID: RUN-2024090411-A3F
Duration: 1m 23s

Total transactions: 342
✅ Auto-matched: 321 (93.9%)
⚠️ Pending review: 21 (6.1%)
Unmatched value: KES 127,450

Reply: unmatched to see exception list
```

---

### `summary <from> <to>` — Period Summary

**Trigger phrases:** `summary 2024-01-01 2024-01-31`, `report Jan 2024`, `monthly summary`

**Response:**

```
📊 *Period Summary*
Tenant: Kimani Superstore
Period: 01 Jan – 31 Jan 2024

Total Inflows:   KES 4,287,500
Total Outflows:  KES 3,950,200
Net Position:    KES 337,300

By Rail:
  M-Pesa:       KES 2,100,000 in / KES 1,890,000 out
  Airtel Money:   KES 487,500 in / KES 430,200 out
  Bank:         KES 1,700,000 in / KES 1,630,000 out

Reconciliation:
  Matched: 98.2%  |  Unmatched: 1.8% (KES 62,430)
```

---

### `help` — Command Menu

**Trigger phrases:** `help`, `?`, `commands`, `what can you do`

**Response:**

```
👋 *PesaGraph Commands*

💰 float         — Current balances across all rails
⚠️ unmatched     — Exception queue summary
✅ resolve <REF> — Reconcile a specific transaction
⚙️ reconcile     — Run auto-reconciliation now
📊 summary <from> <to> — Period report (YYYY-MM-DD)
❓ help          — Show this menu

Visit https://app.pesagraph.co.ke for the full dashboard.
Support: support@pesagraph.co.ke
```

---

## Proactive Alerts (Outbound — No User Prompt Required)

### Daily Float Digest (08:00 EAT)

Sent automatically every morning:

```
☀️ *Good morning, Kimani Superstore*
Daily float report — 04 Sep 2024

Total Float: KES 379,550
Status: 1 rail requires attention

⚠️ Airtel Money: KES 12,200 (LOW — min KES 15,000)

Reply *float* for full details.
```

---

### Low-Float Alert (Real-Time Threshold Breach)

Triggered immediately when a rail drops below `MinimumThreshold`:

```
🚨 *Low Float Alert*
Tenant: Kimani Superstore
Time: 04 Sep 2024, 14:23 EAT

⚠️ Airtel Money balance has dropped below minimum threshold.

Current:  KES 8,900
Minimum:  KES 15,000
Deficit:  KES 6,100

Please top up or rebalance float.
Reply *float* to see all balances.
```

---

### Reconciliation Anomaly Alert

Triggered when unmatched rate exceeds 10% in a run:

```
⚠️ *Reconciliation Alert*
Tenant: Kimani Superstore
Run: RUN-2024090411-A3F

Unmatched rate: 14.2% (above 10% threshold)
Unmatched value: KES 284,300

This may indicate a provider outage or data gap.
Reply *unmatched* to review or visit the dashboard.
```

---

## Error Responses

| Scenario | Response |
| --- | --- |
| Unknown command | `❓ I didn't understand that. Reply *help* for a list of commands.` |
| Invalid ref in resolve | `❌ Transaction ref ABC123 not found. Check the ref and try again.` |
| Unauthorised number | `🔒 This number is not linked to a PesaGraph account. Contact your admin.` |
| Session timeout | `⏱️ Your session expired. Please resend your command to continue.` |
| Provider unavailable | `⚠️ Unable to fetch live balances — provider API is not responding. Data shown may be up to 15 minutes old.` |

---

## Rate Limits

| Action | Limit | Window |
| --- | --- | --- |
| Any outbound message | 1 per 3 seconds | Per phone number |
| `reconcile` command | 1 per 10 minutes | Per tenant |
| `summary` command | 5 per hour | Per tenant |
| Proactive alert (low-float) | 1 per hour | Per rail per tenant |

---

## SMS Fallback

When WhatsApp is unavailable or the user has not opted in:

- All commands work identically via SMS to the tenant's registered shortcode
- Responses are truncated to 160 characters per SMS segment
- Multi-part responses are split and sent sequentially with a 1-second delay
- Formatting (bold, emoji) is stripped for SMS
