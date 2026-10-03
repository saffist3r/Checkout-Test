---
name: dotnet-clean-code
description: C#/.NET 8 clean code conventions for this payment gateway. Use when writing or reviewing any C# in payment-gateway-challenge-dotnet: naming, structure, nullability, async, DI, error handling, and what "simple, not over-engineered" means here.
---

# .NET clean code for the payment gateway

The goal is code a Checkout.com reviewer can read top to bottom in a few minutes. Prefer the obvious solution.

## Style (enforced by the template's `.editorconfig`, do not edit it)

- File-scoped namespaces, 4 spaces, usings sorted with `System` first and separated into groups.
- **Explicit types, no `var`** (Checkout SDK convention; the template's editorconfig also prefers it).
- Always braces, opening brace on a new line. Namespace matches the folder.
- Modern C#: switch expressions, pattern matching, index/range, throw expressions, target-typed `new()`.

**Read `checkout-conventions.md` in this folder** for the full list of conventions taken from Checkout's public `checkout-sdk-net`, and which ones we deliberately did not adopt.
- `private readonly` fields with `_camelCase`; PascalCase for types, methods, properties.
- Expression-bodied members only for one-liners.

## Structure

- **Small classes, one reason to change.** Controller = HTTP mapping. Validator = rules. Service = orchestration. Bank client = wire format + transport errors. Repository = storage.
- **No logic in controllers** beyond: validate → call service → map result to status code.
- **Records for data** (`Payment`, responses, bank DTOs). Classes for things with behaviour or dependencies.
- **Immutable by default.** The stored `Payment` is a record and is never mutated.
- **Guard clauses and early returns** over nested `if`s.
- **No magic values:** supported currencies, card length bounds etc. are named constants in one place.
- **Delete dead code** and template leftovers you replaced. No commented-out code.

## Nullability and types

- `<Nullable>enable</Nullable>` stays on. No `!` unless the invariant is proven a few lines above (e.g. after validation) and commented if not obvious.
- Request DTO fields are nullable so "missing" is distinguishable from `0`.
- Strings for identifiers that look numeric (card number, CVV). `Guid` for payment ids.

## Async and I/O

- `async`/`await` all the way for I/O; pass `CancellationToken` from the controller down to the HttpClient.
- Never `.Result` / `.Wait()`.
- Use `IHttpClientFactory` typed clients (`AddHttpClient<IAcquiringBankClient, AcquiringBankClient>`), base address from configuration, an explicit timeout.

## Errors

- Expected outcomes (validation failure, declined) are **values**, not exceptions.
- Unexpected external failures (bank down) are a **specific `sealed` exception** (`BankUnavailableException`, carrying the bank's HTTP status like Checkout's `CheckoutApiException`) caught once in the controller and mapped to `502`.
- Public methods guard arguments (`ArgumentNullException.ThrowIfNull`), as the SDK does with `ValidateParams`.
- `CancellationToken cancellationToken = default` is always the last parameter.
- Never `catch (Exception)` and swallow. Never return `200` for a failure.

## Dependency injection

- Constructor injection only. Lifetimes: repository and validator `Singleton` (stateless/thread-safe), service `Scoped`, bank client via `AddHttpClient`.
- Inject `TimeProvider` instead of calling `DateTime.Now`.

## What NOT to add (over-engineering checklist)

MediatR/CQRS, AutoMapper, FluentValidation (unless agreed), generic repositories, extra class libraries, a Result<T> framework, custom middleware pipelines, Polly policies, real databases, auth. Mention relevant ones in the README's "what I'd do next" instead.

## Review checklist before committing

1. Builds with no new warnings.
2. Each new public method has a test.
3. Names say what, not how.
4. No PAN/CVV in logs, responses or storage.
5. Diff touches only what the change needs.
