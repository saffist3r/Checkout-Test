---
name: dotnet-testing
description: Testing strategy for the .NET payment gateway with xUnit and WebApplicationFactory. Use when adding or changing tests, faking the acquiring bank, controlling time, or deciding what coverage a change needs.
---

# Testing the payment gateway

## Layers

| Layer | Tool | Fakes | Purpose |
|-------|------|-------|---------|
| Unit: validator | plain xUnit `[Theory]` | `FixedTimeProvider` | Every rule, every boundary |
| Unit: service | xUnit | `FakeAcquiringBankClient`, real `PaymentsRepository` | Status mapping, last-four, bank request mapping (`MM/yyyy`) |
| Unit: bank client | xUnit | `StubHttpMessageHandler` | snake_case wire format, 4xx/5xx/network/bad JSON → `BankUnavailableException` |
| API (in-process) | `WebApplicationFactory<Program>` | fake bank + fixed time via `ConfigureServices` | Status codes, JSON shape, end-to-end flow POST → GET |
| Manual / optional | `docker-compose up` + `.http` file or curl | real simulator | Sanity check before submission |

## Rules

- **Arrange / Act / Assert**, one behaviour per test, named in Checkout's `Should...` style (`ShouldRejectInvalidPaymentWithoutCallingBank`).
- Class names: `PaymentRequestValidatorTest`, `PaymentsServiceTest`, `AcquiringBankClientTest` (unit), `PaymentsControllerIntegrationTest` (WebApplicationFactory).
- Assertions with **Shouldly** (`response.StatusCode.ShouldBe(HttpStatusCode.OK)`), as in checkout-sdk-net.
- **Deterministic:** no `Random`, no `DateTime.Now`, no real network in automated tests.
- **Fake at the seam you own** (`IAcquiringBankClient`), not HttpClient internals, for API tests.
- Swap services with `services.RemoveAll<T>()` then add the fake, inside `WithWebHostBuilder`.
- Assert on **what the merchant sees**: status code, `status` string, fields, and that the full card number and CVV never appear in the body.
- Assert **side effects**: the bank is not called for rejected requests; nothing is stored when the bank is unavailable.
- Use the simulator's rule in fakes: last digit odd → authorized, even → declined, 0 → unavailable.
- Keep test data in one `TestData` helper (valid request builder, test cards, fixed "now").

## Boundaries that must be covered

- Card number length 13 ✗, 14 ✓, 19 ✓, 20 ✗; non-digits ✗; leading zeros kept.
- Expiry month 0 ✗, 1 ✓, 12 ✓, 13 ✗; last month ✗, current month ✓ (documented assumption), next year ✓.
- Currency: each supported ✓, unsupported ✗, lowercase ✗, wrong length ✗.
- Amount: missing ✗, 0 ✗, negative ✗, decimal in JSON ✗ (model binding).
- CVV: 2 ✗, 3 ✓ (incl. leading zero), 4 ✓, 5 ✗, letters ✗.

## Packages

xUnit 2.9, Shouldly, `Microsoft.AspNetCore.Mvc.Testing` 8.x (must match net8.0), `Microsoft.NET.Test.Sdk`, coverlet. No mocking library: Checkout's SDK uses Moq, but hand-written fakes are clearer for our single seam.

## Before claiming done

`dotnet test` green locally. If the SDK is unavailable, say so explicitly. Never report an unrun suite as passing.
