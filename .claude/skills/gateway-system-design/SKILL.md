---
name: gateway-system-design
description: System design for the payment gateway assessment, covering components, request flow, boundaries, scope decisions, and the README's design and assumptions write-up. Use when planning features, changing the architecture, or documenting design decisions.
---

# Payment gateway system design

## Context

```
Shopper → Merchant → [ Payment Gateway (this repo) ] → Acquiring Bank (simulator :8080)
```

The gateway validates, forwards to the bank, records the outcome and lets the merchant read it back.

## Components

```
PaymentsController           POST /api/payments, GET /api/payments/{id}
  ├─ PaymentRequestValidator  pure rules + TimeProvider
  ├─ PaymentsService          map request → bank call → Payment → store
  │    ├─ IAcquiringBankClient (AcquiringBankClient: typed HttpClient, snake_case, error mapping)
  │    └─ PaymentsRepository  ConcurrentDictionary<Guid, Payment>
  └─ PaymentsRepository       read path for GET
```

## Process payment flow

1. Model binding: malformed JSON → 400 Rejected (custom `InvalidModelStateResponseFactory`).
2. Validator: any error → 400 Rejected with all field errors. **Stop.**
3. Service builds the bank request (`MM/yyyy`) and calls the bank.
4. Bank error → `BankUnavailableException` → 502. **Nothing stored.**
5. Bank reply → `Authorized`/`Declined`, new `Guid` id, last four only → store → 200.

## Retrieve payment flow

`GET /{id:guid}` → repository → 404 or 200 with the same response shape as POST.

## Design principles

- **Meet the brief, nothing speculative.** Every class must trace to a requirement or a test seam.
- **Single seam for external I/O** (the bank client) so it can be faked and swapped.
- **Separate domain model from DTOs**: `Payment` (stored, may hold auth code) vs `PaymentResponse` (what merchants see).
- **Fail loudly and explicitly**: unknown bank outcomes are errors, not declines.
- **Thread safety**: repository uses `ConcurrentDictionary`; everything else is stateless.

## Out of scope (write in README, don't build)

Real database, authentication/merchant scoping, idempotency keys (unless approved), retries and reconciliation, rate limiting, 3DS, refunds/captures, multi-currency rules, horizontal scaling, observability stack.

## README structure for the submission

1. How to run (simulator, API, tests).
2. API contract (endpoints, example requests/responses, status code table).
3. Design overview (the component sketch above, in a few lines).
4. Key decisions and assumptions (current-month expiry, currencies, amount > 0, 400 Rejected shape, 502 on bank failure, strings for PAN/CVV, last four as string).
5. Security notes (PAN/CVV handling, logging).
6. What I'd do next with more time (the out-of-scope list, prioritised: idempotency, persistence, auth, observability).
