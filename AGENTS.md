# Agent rules: Checkout.com Payment Gateway assessment (.NET)

These rules apply to every AI agent (Claude Code, Cursor, Copilot…) and every human working in this repository.
Read them before changing code. Detailed guidance lives in `.claude/skills/*/SKILL.md`.

## 1. What this repository is

A take-home technical assessment for Checkout.com: build a small **payment gateway API** in .NET.

| Path | What it is | Can we change it? |
|------|------------|-------------------|
| [cko-recruitment](https://github.com/cko-recruitment/) | Not in this repo. The full brief is the profile README of `cko-recruitment/.github`. | **No.** Read only, never push there. |
| `payment-gateway-challenge-dotnet/` | The .NET template, brought in as a regular folder (with its history) so the solution can be pushed to our own repo. **All solution code goes here.** | Yes, except the files below. |
| `payment-gateway-challenge-dotnet/imposters/` | Bank simulator (Mountebank) config. | **No.** |
| `payment-gateway-challenge-dotnet/.editorconfig` | Formatting rules used to grade submissions. | **No.** |
| `payment-gateway-challenge-dotnet/docker-compose.yml` | Starts the simulator. | Only if strictly needed; prefer not. |

**The brief is the spec.** When these rules and the brief disagree, the brief wins. When the brief is silent, pick the simplest reasonable option, write it down in the solution README's *Assumptions* section, and move on.

## 2. Hard constraints from the assessment

1. **It must compile** and `dotnet test` must be green before any commit.
2. **Automated tests** cover the behaviour (see §7).
3. **Simple and maintainable. No over-engineering.** Reviewers explicitly penalise it. No MediatR, CQRS, repositories-of-repositories, generic base classes, AutoMapper, or extra projects "for later".
4. **Focus on the functional requirements**: process a payment (Authorized / Declined / Rejected) and retrieve a payment by id.
5. **Document key design decisions and assumptions** in `payment-gateway-challenge-dotnet/README.md`.
6. **Never open pull requests or push to `cko-recruitment` repositories.** Our remote is `git@github.com:saffist3r/Checkout-Test.git`.

## 3. Workflow rules for agents

- **Plan first, then code.** For anything beyond a one-file change, state the plan and the files you will touch before editing.
- **Ask, don't guess, on product decisions** (status codes, response shapes, accepted currencies). Pick a default, say which, and flag it.
- **Small, reviewable commits** with imperative messages (`Add card number validation`). One concern per commit. Never commit failing tests.
- **Run before you claim done:** `dotnet build` with zero warnings in new code, `dotnet test`, and for bank-facing changes a manual run against the simulator.
- **Do not touch unrelated code** or reformat files you did not change.
- **Never put real card data anywhere** (code, tests, logs, commit messages). Use the simulator's test cards and obviously fake numbers.
- **Keep secrets out of the repo.** Config comes from `appsettings*.json` or environment variables; nothing sensitive is committed.
- If a rule here blocks you, stop and ask rather than working around it.

## 4. Checkout.com house style

We follow the conventions of Checkout's own public .NET code ([checkout-sdk-net](https://github.com/checkout/checkout-sdk-net)) where they fit. Full mapping: `.claude/skills/dotnet-clean-code/checkout-conventions.md`. In short:

- Explicit types (no `var`), always braces, Allman style, namespace = folder, accessibility modifiers everywhere, `readonly _camelCase` fields.
- Modern C#: switch expressions, pattern matching, `[^4..]`, throw expressions, target-typed `new()`.
- Clients are `IXxxClient` / `XxxClient` with path constants. `CancellationToken cancellationToken = default` is always the last parameter.
- Exceptions are `sealed` and carry context (HTTP status). Public methods guard their arguments.
- Card number and CVV are strings, expiry is integers, and only the last four digits are returned. Amounts are integer minor units. Status enums serialize as strings.
- Tests: `Should...` method names, `*Test` / `*IntegrationTest` classes, Shouldly assertions.
- Idempotency header name, if built: `Cko-Idempotency-Key`.
- `global.json` pins the 8.0 SDK.
- If the SDK conflicts with the template's `.editorconfig` (tabs vs spaces), the template wins.

## 5. Architecture (keep it this small)

```
src/PaymentGateway.Api
  Controllers/        HTTP only: bind, call, map to status codes. No business logic.
  Models/             Domain record (Payment), PaymentStatus enum, request and response DTOs.
  Services/           PaymentRequestValidator, Iso4217 (local code list), PaymentsService (orchestration), PaymentsRepository (in-memory), PaymentMetrics (outcome counter).
  Services/Bank/      IAcquiringBankClient + typed HttpClient implementation and bank DTOs.
test/PaymentGateway.Api.Tests
  Unit tests per class + in-process API tests with WebApplicationFactory.
```

- **One API project, one test project.** Folders, not projects, for separation.
- **Dependencies point inward:** Controller → Service → (Bank client, Repository). The bank client is the only thing that knows the bank's wire format.
- **Interfaces only at real seams:** the acquiring bank (external I/O). Do not add interfaces for classes with one implementation that tests can construct directly.
- **Use built-in .NET**: `IHttpClientFactory` typed clients, `TimeProvider`, `System.Text.Json`, `ILogger`, options/config. No extra NuGet packages without a stated reason.

## 6. API contract decisions

| Situation | Response |
|-----------|----------|
| Valid request, bank authorizes | `200 OK`, payment body with `status: "Authorized"` |
| Valid request, bank declines | `200 OK`, payment body with `status: "Declined"` |
| Invalid request (validation or malformed JSON) | `400 Bad Request`, `{ "status": "Rejected", "errors": { field: [messages] } }`. **The bank is not called and nothing is stored.** |
| Bank unavailable (5xx, timeout, network, unreadable reply) | `502 Bad Gateway` ProblemDetails. Nothing is stored. |
| `GET` unknown id | `404 Not Found` |
| `GET` known id | `200 OK`, same payment body |

- Routes: `POST /api/payments`, `GET /api/payments/{id:guid}`.
- JSON is camelCase; enums serialize as **strings**.
- Payment body fields: `id`, `status`, `cardNumberLastFour` (string, keeps leading zeros), `expiryMonth`, `expiryYear`, `currency`, `amount`.

## 7. Testing rules

- Every validation rule has a passing and a failing test, including boundaries (13/14/19/20 digits, month 0/1/12/13, current month).
- Every response row in §6 has an API-level test through `WebApplicationFactory<Program>`.
- The bank is faked at the `IAcquiringBankClient` seam for API tests; the real `AcquiringBankClient` is tested with a stub `HttpMessageHandler` (wire format, error mapping).
- Time is injected via `TimeProvider`; tests never depend on today's date.
- Tests are deterministic: no `Random`, no real network, no sleeps.
- Test names follow Checkout's `Should...` style: `ShouldRejectExpiredCard`, `ShouldReturnNotFoundForUnknownPayment`. Unit test classes end in `Test`, API-level ones in `IntegrationTest`. Assertions use Shouldly.

## 8. Payments and security rules (non-negotiable)

- **Never store, log or return the full card number or the CVV.** Keep only the last four digits after the bank call.
- **Money is an integer in minor units** (`int Amount`, 1050 = £10.50). Never `double`/`float`/`decimal` for amounts in this API.
- **Card number and CVV are strings** (leading zeros, 19 digits).
- **Validate before calling the bank**; reject early with every error at once.
- **Explicit status mapping:** only a successful bank reply produces `Authorized` or `Declined`. Any failure is an error, never a silent decline.
- Logs carry payment id, status and bank status code, never PAN, CVV or full request bodies.

See `.claude/skills/payments-fintech/SKILL.md` for the reasoning and what to mention (but not build) in the README.

## 9. Commands

```bash
cd payment-gateway-challenge-dotnet
docker-compose up -d          # bank simulator on http://localhost:8080
dotnet build
dotnet test
dotnet run --project src/PaymentGateway.Api   # Swagger at /swagger in Development
```
