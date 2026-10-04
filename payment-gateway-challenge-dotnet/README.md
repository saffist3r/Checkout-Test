# Payment Gateway (.NET)

A small ASP.NET Core API that lets a merchant **process a card payment** through an acquiring bank and **retrieve a payment** later.
This is my solution to the Checkout.com payment gateway challenge ([brief](https://github.com/cko-recruitment/)).

## Running it

Requirements: .NET 8 SDK, Docker.

```bash
docker-compose up -d                               # bank simulator on http://localhost:8080
dotnet test                                        # unit + in-process API tests (no simulator needed)
dotnet run --project src/PaymentGateway.Api        # API on http://localhost:5067, Swagger at /swagger
```

`src/PaymentGateway.Api/PaymentGateway.Api.http` has ready-made requests for every outcome. The bank URL is configured with `AcquiringBank:BaseUrl` in `appsettings.json`.

## Demo UI

A one-page UI to try the gateway end to end, with the bank simulator, API and UI all in Docker:

```bash
docker-compose -f docker-compose.yml -f docker-compose.demo.yml up --build
```

Open http://localhost:3000. Scenario buttons fill in an authorized, declined, bank-unavailable or invalid payment. Each response is shown with its HTTP code and a plain explanation, and payments made in the session can be retrieved by id. The API is also exposed directly on http://localhost:5080, with Swagger at http://localhost:5080/swagger (linked from the page header).

nginx serves `demo/index.html` and proxies `/api` to the gateway, so the API needs no CORS setup. The demo is kept out of the API project on purpose.

## API

### `POST /api/payments`

```json
{ "cardNumber": "2222405343248877", "expiryMonth": 4, "expiryYear": 2027, "currency": "GBP", "amount": 1050, "cvv": "123" }
```

| Outcome | HTTP | Body |
|---------|------|------|
| Bank authorized | `200` | `{ "id", "status": "Authorized", "cardNumberLastFour": "8877", "expiryMonth", "expiryYear", "currency", "amount" }` |
| Bank declined | `200` | same, `"status": "Declined"` |
| Invalid request | `400` | `{ "status": "Rejected", "errors": { "cardNumber": ["..."], ... } }` |
| Bank unavailable | `502` | ProblemDetails |

### `GET /api/payments/{id}`

`200` with the same payment body, or `404` if the id is unknown.

## Design

```
PaymentsController          HTTP only: validate, call the service, map to a status code
 ├─ PaymentRequestValidator  all field rules; time comes from TimeProvider
 ├─ PaymentsService          builds the bank request, maps the reply to a Payment, stores it
 │   ├─ IAcquiringBankClient typed HttpClient; owns the bank's snake_case wire format and error mapping
 │   └─ PaymentsRepository   in-memory ConcurrentDictionary (the brief allows a test double)
 └─ PaymentsRepository       read path for GET
```

- **One project, folders for separation.** The brief asks for simple, maintainable code, so there is no MediatR, AutoMapper, extra class libraries or generic repository.
- **One interface, at the only external boundary** (the bank). It is what the API tests fake. Everything else is concrete and constructed directly in unit tests.
- **The stored domain record (`Payment`) is separate from the response DTO (`PaymentResponse`).** The authorization code is stored but not exposed.
- **Expected outcomes are values and failures are exceptions.** Rejected and Declined are normal results. A bank failure is a `BankUnavailableException`, caught once in the controller and turned into `502`.
- The style follows Checkout's public .NET SDK where it fits the template's `.editorconfig`: explicit types, `Should...` test names, Shouldly, `sealed` exceptions carrying the HTTP status, and `CancellationToken` as the last parameter.

## Key decisions and assumptions

| Topic | Decision |
|-------|----------|
| Rejected response | `400` with `status: "Rejected"` and **every** field error at once. The bank is not called and nothing is stored, so there is no id. Malformed JSON (e.g. `"amount": 10.5`) returns the same shape. |
| Bank failure | Bank `5xx`, `4xx`, timeout (10s), network error or unreadable body → `502`, and nothing is stored. This is **not** a decline: the outcome is unknown and the merchant may retry. The bank returning `400` would mean a gateway bug, so it is treated the same way and logged. |
| Card number / CVV types | Strings, so leading zeros survive and 19 digits fit (the template used `int`). |
| Last four digits | A string (`"0877"`, not `877`). |
| Expiry | Month must be 1–12 and month+year must not be in the past. **A card expiring this month is accepted**, because cards are valid until the end of their expiry month. |
| Currencies | Two checks. The code must be an uppercase ISO 4217 code, checked against a local copy of the list (`Iso4217.cs`) so no external service is called per payment. It must then be one of `GBP`, `USD`, `EUR` (the brief caps the list at three). So `XYZ` and `JPY` get different errors. |
| Amount | Integer in minor units and **must be > 0**. The brief says "integer"; a zero or negative payment makes no sense. `int` matches the template; a production system would use `long`, as Checkout's SDK does. |
| Luhn check | Not applied. The brief doesn't require it and the simulator's test cards don't need it. |
| Status values | Serialized as strings (`"Authorized"`), not enum numbers. |
| Error field names | camelCase, matching the JSON request. |

## Security

- The full card number is used only in memory to call the bank. It is never stored, logged or returned, and only the last four digits are kept.
- The CVV is never stored, in line with PCI DSS rules on sensitive authentication data.
- Logs contain the payment id, status and bank HTTP status, never request bodies.
- A test asserts that responses never contain the full card number or the CVV.

## Tests

62 tests, all deterministic (fixed `TimeProvider`, no network):

- `PaymentRequestValidatorTest`: every rule with its boundaries (13/14/19/20 digits, month 0/1/12/13, last vs current month, currency ISO 4217 vs supported, CVV length and characters).
- `PaymentsServiceTest`: status mapping, last-four extraction, and the bank request (`MM/yyyy` expiry).
- `AcquiringBankClientTest`: snake_case wire format, and `4xx`/`5xx`, network failure and bad JSON all becoming `BankUnavailableException`. Uses a stub `HttpMessageHandler`.
- `PaymentsControllerIntegrationTest`: `WebApplicationFactory` with a fake bank, covering every row of the API table, the POST → GET round trip, rejection without calling the bank, no card number or CVV in responses, and the Swagger document listing both endpoints.

### End-to-end user flows

`e2e/` holds 6 Playwright tests that use the demo page the way a merchant would, against the real stack (UI, gateway, bank simulator): pay and retrieve an authorized payment, see a declined one stored, get "bank unavailable" with nothing stored, get every validation error at once, fix a rejected payment and resubmit, and look up an unknown id. They live outside the .NET solution because they test the running system, not the code.

```bash
docker-compose -f docker-compose.yml -f docker-compose.demo.yml up -d --build
cd e2e && npm ci && npx playwright install chromium && npm test
```

CI runs them in a separate job and posts a summary of each flow.

## What I'd do next

1. **Idempotency.** Accept a `Cko-Idempotency-Key` header and return the stored result for a repeated key, so a merchant retrying after a timeout can't charge the card twice. This is the most important gap for a real gateway.
2. **Unknown outcomes.** If the bank times out after receiving the request, the payment may have gone through. Record it as pending and reconcile with the bank instead of returning only `502`.
3. **Persistence.** A durable store, encryption at rest, and `long` amounts.
4. **Merchant authentication.** API keys, with payments scoped to the merchant that created them so one merchant can't read another's.
5. **Observability.** Structured logs with correlation ids (e.g. a `Cko-Request-Id` response header), metrics on bank latency and error rate, and tracing.
6. **Resilience.** A circuit breaker on the bank client. Retries only where safe: never blindly retry an authorization.
