---
name: payments-fintech
description: Fintech and card-payments best practices for the Checkout.com gateway assessment. Use when handling card data, amounts, currencies, payment statuses, bank failures, idempotency, logging, or writing the README's security and design sections.
---

# Payments and fintech practices

Checkout.com reviewers are payments engineers. Getting these right signals domain awareness more than any pattern.

## Card data (PCI DSS mindset)

- **PAN (full card number):** used only in memory to call the bank. Never stored, logged or returned. Keep **last four** only (as a string).
- **CVV:** never stored in any form, even encrypted (PCI DSS forbids storing sensitive authentication data after authorization). Only forwarded to the bank.
- Expiry, currency, amount and last four are fine to store and return.
- Logs: payment id, status, bank HTTP status, latency. Never request bodies. Don't log model-binding errors that echo the raw PAN.
- Test data: simulator test cards only (`2222405343248877` authorized, `…112` declined, `…110` unavailable).

## Money

- **Integer minor units** (`int Amount`): 1050 GBP = £10.50. No floating point anywhere. Checkout's SDK uses `long? Amount`; we keep the template's `int` and note `long` as the production choice.
- Amount must be > 0 (documented assumption; the brief only says "integer").
- The currency is validated against an allow-list of **3 ISO 4217 codes** (brief limit): `GBP`, `USD`, `EUR`. Uppercase only.
- Never convert currencies or reformat amounts; pass them through to the bank unchanged.

## Statuses and outcomes

- `Authorized` / `Declined` come **only** from a successful bank response.
- `Rejected` means the gateway refused the request **before** calling the bank. No id, nothing stored.
- Bank failure (5xx, timeout, network, unreadable reply) is **not** a decline. Return `502` so the merchant knows the outcome is unknown and can retry. Do not store a payment.
- Bank `400` means our request was malformed (our bug): treat as unavailable (`502`) and log it.

## Validation

- Validate everything before the bank call, return **all** errors at once, keyed by field.
- Expiry: month 1–12; month+year not in the past. The card is valid through the end of its expiry month, so the current month is accepted (documented).
- Card number 14–19 digits only; CVV 3–4 digits only. No Luhn check (the simulator's test cards aren't required to pass it; mention as a possible addition).

## Reliability topics: mention in README, build only if agreed

| Topic | Production approach | In this assessment |
|-------|--------------------|--------------------|
| Idempotency | `Cko-Idempotency-Key` header (the name Checkout's own SDK sends); same key returns the same result, prevents double charges on retry | Describe; optional small implementation if the user approves |
| Retries | Retry only idempotent/safe failures with backoff; never blindly retry an authorization | Don't retry; return 502 |
| Timeouts | Explicit HttpClient timeout | 10s timeout |
| Unknown outcome | Reconcile with the bank (timeout after send) | Document as a known gap |
| Persistence | Durable store, encryption at rest | In-memory test double (allowed by the brief) |
| AuthN/AuthZ | Merchant API keys, payments scoped to merchant | Out of scope, document |
| Observability | Structured logs, metrics, tracing with payment id correlation | `ILogger` with payment id |

## Wire format to the simulator

`POST http://localhost:8080/payments`, snake_case:
`card_number`, `expiry_date` (`MM/yyyy`, zero-padded), `currency`, `amount`, `cvv` → `{ authorized, authorization_code }`.
