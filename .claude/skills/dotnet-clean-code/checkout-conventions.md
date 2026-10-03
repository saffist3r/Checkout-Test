# Conventions taken from Checkout.com's public .NET code

Source: https://github.com/checkout/checkout-sdk-net (read at commit `db881f8`). Only conventions that fit a small ASP.NET Core API are adopted.
When the SDK and the assessment template's `.editorconfig` disagree (e.g. the SDK indents with tabs, the template with spaces), **the template wins**.

## Adopted

| Area | Checkout SDK does | We do |
|------|-------------------|-------|
| Explicit types | `.editorconfig`: `csharp_style_var_* = false` | Explicit types, no `var` (the template's editorconfig says the same) |
| Braces | `csharp_prefer_braces = true`, Allman (`new_line_before_open_brace = all`) | Always braces, opening brace on its own line |
| Modern C# | prefers switch expressions, pattern matching, index/range (`[^4..]`), throw expressions, target-typed `new()` | Same |
| Modifiers | `require_accessibility_modifiers`, `readonly` fields, `_camelCase` | Same |
| Namespaces | `dotnet_style_namespace_match_folder = true` | Namespace always matches the folder |
| Usings | outside namespace | Same (template sorts System first in groups) |
| Client shape | `IPaymentsClient` / `PaymentsClient`, path as `private const string PaymentsPath = "payments"` | `IAcquiringBankClient` / `AcquiringBankClient`, path constant |
| Method signature | `RequestPayment(request, string idempotencyKey = null, CancellationToken cancellationToken = default)` | `CancellationToken cancellationToken = default` is always the last parameter |
| Idempotency | `Cko-Idempotency-Key` request header | Same header name if idempotency is built |
| Correlation | reads `Cko-Request-Id` from responses, puts it on exceptions | Log the payment id; optionally return a request id |
| Exceptions | base `CheckoutException`; `CheckoutApiException` carries `RequestId`, `HttpStatusCode`, `ErrorDetails`; `sealed` | `sealed BankUnavailableException` carrying the bank's `HttpStatusCode?` |
| Argument checks | `CheckoutUtils.ValidateParams(...)` throws `CheckoutArgumentException` at public entry points | Guard public methods with `ArgumentNullException.ThrowIfNull` |
| Card model | `Number` string, `ExpiryMonth`/`ExpiryYear` `int?`, `Cvv` string; responses expose `Last4` string, never the PAN | Same types; field named `CardNumberLastFour` as in the brief |
| Amounts | `long? Amount` in minor units | Integer minor units (`int` per the template; `long` is the production choice, noted in README) |
| Status enum | `PaymentStatus` serialized as strings ("Authorized", "Declined") | Strings via `JsonStringEnumConverter` |
| Test naming | `Should...` methods, e.g. `ShouldRequestPayment`; classes `*Test` for unit, `*IntegrationTest` for API-level | Same |
| Assertions | Shouldly (`response.ShouldNotBeNull()`) | Shouldly |
| SDK pinning | `global.json` with `8.0.x` and `rollForward` | Add `global.json` |

## Not adopted (and why)

- **Tabs and CRLF**: the template's `.editorconfig` is spaces, and we must not change it.
- **Moq**: hand-written fakes are clearer for one seam and avoid a dependency.
- **Multi-target CI matrix (netcoreapp3.1 to net8)**: an SDK concern; we target net8.0 only.
- **Abstract base clients and builders** (`AbstractClient`, `CheckoutSdkBuilder`): they exist to serve dozens of resources. With one bank call they would be over-engineering.
