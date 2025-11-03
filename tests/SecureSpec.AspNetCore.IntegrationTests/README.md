# SecureSpec.AspNetCore.IntegrationTests

This project hosts the end-to-end integration test suite. It boots the dedicated SecureSpec test host, captures canonical JSON/YAML fixtures, and validates the embedded UI through Chromium + Playwright.

## Prerequisites

- .NET 8 SDK
- Playwright browsers. Install once via:
  ```fish
  dotnet tool update --global Microsoft.Playwright.CLI
  ~/.dotnet/tools/playwright install chromium
  ```

## Running the tests

```fish
# Regenerate fixtures (optional in CI; required when schema changes)
dotnet run --configuration Release --project tests/SecureSpec.AspNetCore.IntegrationTests -- regenerate-fixtures

# Execute the full test suite (headless Chromium by default)
dotnet test --configuration Release tests/SecureSpec.AspNetCore.IntegrationTests
```

To debug the UI in a headed browser, set `SECURESPEC_PLAYWRIGHT_HEADED=1` before running tests or Playwright scripts.

## Fixtures

Canonical OpenAPI documents live under `Fixtures/` and are updated automatically by the regeneration command. Always regenerate and commit updated fixtures whenever the schema surface changes.
