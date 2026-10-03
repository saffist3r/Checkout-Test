# CLAUDE.md

@AGENTS.md

## Claude Code specifics

- Project skills live in `.claude/skills/`. Load the matching one before working in its area:
  - `dotnet-clean-code`: C# style, naming, structure, what "simple" means here.
  - `dotnet-testing`: xUnit, WebApplicationFactory, fakes, what to test.
  - `payments-fintech`: card data handling, money, idempotency, failure modes.
  - `gateway-system-design`: how the gateway fits together and what is deliberately out of scope.
- The .NET SDK (8.0) must be installed for `dotnet build`/`test`. If it is missing, say so instead of claiming the build passed.
- The folder name ends with a space (`Checkout Test `); always quote paths.
