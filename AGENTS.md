# Repository Guidelines

## Project Structure & Module Organization
- `src/QuoteEngine.Api/`: ASP.NET Core Web API (controllers, services, middleware, models).
- `src/QuoteEngine.Data/`: EF Core data layer (DbContext, entities, migrations, repositories).
- `src/QuoteEngine.Functions/`: Azure Functions examples.
- `src/QuoteEngine.Tests/`: xUnit unit/integration tests for API and services.
- `client/`: Angular 17 frontend (`src/app` for pages/components/services, `e2e/` for Playwright tests).
- `database/`: SQL schema, seed data, and optimization scripts.
- `docs/` and `deploy/`: architecture and deployment references.

## Build, Test, and Development Commands
- Backend restore/build/test:
  - `dotnet restore`
  - `dotnet build --no-restore`
  - `dotnet test --no-build`
- Run API locally:
  - `dotnet run --project src/QuoteEngine.Api`
- Frontend setup/dev:
  - `cd client && npm ci`
  - `npm start` (Angular dev server on `http://localhost:4200`)
  - `npm run build`
  - `npm run lint`
  - `npm test`
- E2E tests (from `client/`):
  - `npx playwright install --with-deps chromium`
  - `npx playwright test`

## Coding Style & Naming Conventions
- C#: use PascalCase for types/methods/properties, camelCase for locals/parameters, `_camelCase` for private fields, UPPER_CASE for constants.
- TypeScript/Angular: use camelCase for variables/functions and PascalCase for classes/types.
- Enforce Angular selector rules via ESLint:
  - Components: `app-` prefix, kebab-case element selectors.
  - Directives: `app` prefix, camelCase attribute selectors.
- Follow SOLID-oriented service boundaries (see `CONTRIBUTING.md`).

## Testing Guidelines
- Backend tests use xUnit + Moq; place tests under `src/QuoteEngine.Tests/` (e.g., `Services/*Tests.cs`, `Controllers/*Tests.cs`).
- Frontend unit tests run with Karma/Jasmine via `npm test`.
- E2E coverage uses Playwright specs in `client/e2e/*.spec.ts`.
- No hard coverage gate is defined; include tests for changed logic and regression-prone paths.

## Commit & Pull Request Guidelines
- Use Conventional Commits (examples: `feat: ...`, `fix: ...`, `test: ...`, `docs: ...`, `refactor: ...`, `ci: ...`).
- Before opening a PR, run backend tests, frontend tests, lint, and builds.
- PRs should include:
  - concise summary and change type,
  - testing performed,
  - checklist confirmation (conventions followed, self-review, docs updated if needed).
