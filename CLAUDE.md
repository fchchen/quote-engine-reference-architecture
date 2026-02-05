# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Commercial insurance quoting platform with an Angular 17.3 frontend and .NET 8 API backend. Signal-based state management (no NgRx). In-memory data services for demo purposes with interfaces ready for database-backed implementations.

## Architecture

- **Angular 17.3 client**: `client/` — standalone components, Angular Material 17.3 (MDC), signal-based state (`signal`, `computed`, `effect`, `input`, `output`)
- **.NET 8 Web API**: `src/QuoteEngine.Api/` — controllers, services, middleware
- **EF Core Data Layer**: `src/QuoteEngine.Data/` — DbContext, entities, repository pattern
- **Azure Functions**: `src/QuoteEngine.Functions/` — serverless endpoints with MockDataService
- **.NET Tests**: `src/QuoteEngine.Tests/` — unit tests (xUnit + Moq) and integration tests (WebApplicationFactory)
- **E2E Tests**: `client/e2e/` — Playwright specs

### Backend Service Lifetime

Registered in `src/QuoteEngine.Api/Program.cs`:
- **Singleton**: `RiskCalculator`, `InMemoryRateTableService`, `InMemoryBusinessLookupService` (stateless, thread-safe)
- **Scoped**: `QuoteService` (per-request, uses other services)

### Frontend State Pattern

Per-business data isolation uses `Map<string, T>` signal patterns. Form values are saved to maps when switching businesses and restored when returning. The `QuoteFormComponent` orchestrates all child components.

## Build & Run Commands

### .NET API
```bash
dotnet build                                          # Build solution
dotnet run --project src/QuoteEngine.Api               # Run API on http://localhost:5210
dotnet test                                           # Run all .NET tests
dotnet test --filter "FullyQualifiedName~ClassName"    # Run specific test class
dotnet test --filter "FullyQualifiedName~MethodName"   # Run single test
```

### Angular Client
```bash
cd client
npm install                                           # Install dependencies
npm start                                             # Serve on http://localhost:4200
npm run build                                         # Production build
npm run lint                                          # ESLint
npm test                                              # Karma unit tests
```

### Playwright E2E
```bash
cd client
npx playwright install                                # Install browsers (first time)
npx playwright test                                   # Run all E2E tests
npx playwright test quote-form.spec.ts                # Run specific spec
npx playwright test -g "test name"                    # Run single test by name
```

### Environment URLs
- Angular dev: `http://localhost:4200`
- .NET API: `http://localhost:5210/api/v1`
- Azure Functions local: `http://localhost:7071/api`
- Swagger (dev only): `http://localhost:5210/swagger`

## Testing Guidelines

### .NET Tests (xUnit + Moq)
- Always follow TDD (Red-Green-Refactor).
- Use `dotnet test` for running tests.
- Use `xUnit` and `FluentAssertions`.
- Never delete existing tests unless explicitly asked.
- Unit tests use Moq for dependency isolation (AAA pattern: Arrange, Act, Assert).
- Integration tests use `WebApplicationFactory<Program>` for full HTTP pipeline testing.
- When deserializing API responses in integration tests, use `JsonSerializerOptions` with `JsonStringEnumConverter` to match the API's enum-as-string serialization.

### Angular E2E Tests (Playwright)
- Always follow TDD (Red-Green-Refactor).
- Use `npx playwright test` from the `client/` directory.
- Test specs live in `client/e2e/`.
- Never delete existing tests unless explicitly asked.
- Angular Material selectors: `mat-option` for autocomplete results, `.mat-step-header` for stepper navigation.
- Use `{ force: true }` when clicking step headers or elements covered by overlays (e.g., matTooltip).
- Target `label` (not the checkbox element) when clicking `mat-checkbox`.
- Assert checkbox state with `.locator('input').toBeChecked()`, not CSS classes.
- Linear stepper requires step-by-step navigation (cannot skip steps).
- Workers' Compensation contains an apostrophe — use regex `/Workers/` for matching.

## Angular Signals Pitfalls (Angular 17.3)

### NG0600: Writing signals inside effects
Angular 17.3 does NOT allow signal writes inside `effect()` by default — throws NG0600 at runtime (no build error). Must pass `{ allowSignalWrites: true }` to `effect()` options.

### Transitive dependency tracking
Any signal read inside an effect callback (including through called methods) creates a tracked dependency. Use `untracked(() => { ... })` to wrap all non-trigger logic:
```typescript
effect(() => {
  const trigger = this.resetTrigger(); // ONLY tracked signal
  if (trigger > 0) {
    untracked(() => {
      // All reads and writes here are untracked
    });
  }
}, { allowSignalWrites: true });
```

## API JSON Conventions
- Properties use camelCase (`PropertyNamingPolicy.CamelCase`)
- Enums serialize as strings (`JsonStringEnumConverter`)
- Model validation via data annotations on `QuoteRequest` (e.g., `[Range(1, 50)]` on `YearsInBusiness`)
