# Checkout.com Payment Gateway challenge (.NET)

[![Tests](https://github.com/saffist3r/Checkout-Test/actions/workflows/tests.yml/badge.svg)](https://github.com/saffist3r/Checkout-Test/actions/workflows/tests.yml)

The solution is in **[payment-gateway-challenge-dotnet/](payment-gateway-challenge-dotnet/README.md)**. Its README explains how to run it, the API, the design and the assumptions.

- `assessment/` is a submodule of the original brief (`cko-recruitment/.github`). Fetch it with `git submodule update --init`.
- Demo UI: `cd payment-gateway-challenge-dotnet && docker-compose -f docker-compose.yml -f docker-compose.demo.yml up --build`, then open http://localhost:3000.
- `AGENTS.md`, `CLAUDE.md` and `.claude/skills/` are the rules and conventions used while building it.
