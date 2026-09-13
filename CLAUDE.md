# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`recipe-auth-api` is the authentication/identity microservice for the "Kjøkkenhylla" platform (a recipe/meal-planning ecosystem). It is one of several microservices (siblings referenced in code/docs: `recipe-infrastructure`, `recipe-notification-service`, `recipe-core-api`, `recipe-analytics-service`) and is reached in production only through an API Gateway, which prefixes all requests with `/api/auth/*`.

Stack: .NET 10, Clean Architecture, CQRS via MediatR, ASP.NET Core Identity + OpenIddict (OAuth2/OIDC), EF Core + PostgreSQL, MassTransit over RabbitMQ, Quartz.NET, Serilog + Seq.

## Commands

Local infrastructure (Postgres, RabbitMQ, MongoDB, Seq, Mailpit) lives in a separate repo and must be running first:
```bash
git clone https://github.com/theapicat/recipe-infrastructure.git
cd recipe-infrastructure && docker compose up -d
```

Required local secrets (never put these in appsettings.json):
```bash
cd API
dotnet user-secrets set "Authentication:Google:ClientId" "..."
dotnet user-secrets set "Authentication:Google:ClientSecret" "..."
dotnet user-secrets set "JWT:SecretKey" "..."
dotnet dev-certs https --trust
```

Database (migrations project is `Persistence`, startup project is `API`):
```bash
dotnet ef database update --project Persistence --startup-project API
dotnet ef migrations add <Name> --project Persistence --startup-project API
```
On startup, `Program.cs` auto-runs `IdentitySeeder` (creates `Admin`/`User` roles and the initial admin account from `AdminUser` config) and registers `OpenIddictSeeder` as a hosted service (creates the `recipe-web-app` and `recipe-mobile-app` OAuth2 clients).

Build / run:
```bash
dotnet build
dotnet run --project API          # http://localhost:5001 / https://localhost:7001
```

Tests:
```bash
dotnet test Tests/Tests.csproj
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~RegisterUserCommandHandler"
```
Note: `Tests.csproj` is currently a bare skeleton (no test files, no project references yet), even though `Documentation/06-test-strategy.md` describes a full 4-layer strategy (MediatR handlers with mocked dependencies + EF Core InMemory; MassTransit consumers via its in-memory test harness; Quartz job logic with time-shifted user state; controller/OpenIddict integration tests via `WebApplicationFactory`). Intended tooling per that doc: xUnit, NSubstitute, Shouldly. When adding the first real tests, wire up `ProjectReference`s to `Application`/`Persistence`/`API` etc.

## Architecture

Five projects, dependency flow `API → Application → (Persistence, Contracts) → Domain`:

- **`Domain`** — entities (`ApplicationUser` extends `IdentityUser<Guid>`, `BlacklistedEntry`), enums, DTOs (request/response), `Options` classes bound from config. No project dependencies.
- **`Contracts`** — plain event records published/consumed over MassTransit, grouped by origin: `Events/UserActions`, `Events/AdminActions`, `Events/SystemActions`, plus `InvalidEmailDetectedEvent`. No logic, just data contracts shared with other services.
- **`Persistence`** — `ApplicationDbContext` (EF Core + Identity + OpenIddict stores over PostgreSQL, keyed by `Guid`), migrations, `IdentitySeeder`/`OpenIddictSeeder`.
- **`Application`** — all business logic, organized as CQRS under `Application/Mediator/{Account,Admin,Authorization}/{Commands,Queries}/<Feature>/`. Each feature folder has a `*Command`/`*Query`, its `*Handler`, and a `*Result` class. Handlers depend directly on `UserManager<ApplicationUser>`, `SignInManager`, `ApplicationDbContext`, and `IPublishEndpoint` — there's no repository abstraction inverting this, so `Application` has a real `ProjectReference` on `Persistence` (a deliberate pragmatic deviation from textbook Clean Architecture, not an oversight).
- **`API`** — HTTP entry point: `Controllers/` (thin — build a MediatR command/query, map the `Result` to an HTTP status, nothing else), `Consumers/` (MassTransit, e.g. `InvalidEmailDetectedConsumer`), `Jobs/` (Quartz, `AccountLifecycleJob`), `Extensions/` (all DI/startup wiring, one file per concern — read these to see how anything is configured), `Program.cs` (thin composition root).

### The result pattern

Business errors are never thrown as exceptions. Every handler returns a `<Feature>Result` with an `IsSuccess` flag plus situational flags (`IsForbidden`, `IsNotFound`, `IsAlreadyConfirmed`, etc.), an `ErrorMessage`, and/or `IdentityError[]`. Controllers map these flags to HTTP status codes. This shape is duplicated per-feature rather than shared via a common base/generic `Result<T>` — when adding a new command/query, copy the pattern from a neighboring feature rather than reusing a shared type (none exists yet).

### Auth flow

- Token issuance goes through `AuthorizationController` → `/api/auth/connect/token`, handling `grant_type=password` (`PasswordGrantCommandHandler`) and `grant_type=refresh_token` (`RefreshTokenGrantCommandHandler`). Both ultimately build a fresh `ClaimsPrincipal` via `TokenService` so role/claim changes are picked up on every reissue.
- Google OAuth2 is handled by `AccountController`'s `/external-login` (Challenge) and `/external-login-callback` endpoints, which delegate to `ProcessGoogleCallbackCommandHandler`: checks the blacklist first, links Google to an existing account or auto-registers a new one (`EmailConfirmed = true` since Google verified it already), then issues tokens and redirects to the frontend.
- JWT signing is HMAC-SHA256 via `JWT:SecretKey`; access tokens default to 60 min, refresh tokens to 14 days (`JwtOptions`). Identity requires 8+ char passwords with a digit and uppercase, no special-char requirement; email is the unique identifier.
- Current-user extraction in `AccountController` reads `ClaimTypes.NameIdentifier`, then the OpenIddict subject claim, then falls back to an `X-User-Id` header — that header fallback assumes the API Gateway always strips/overwrites it before requests reach this service.

### Email blacklist

`BlacklistedEntries` (exact email or domain patterns) is checked on registration and on the Google callback. It's populated both manually via `AdminController` (`/blacklist`) and automatically: `InvalidEmailDetectedConsumer` listens for `InvalidEmailDetectedEvent` (hard bounces from `recipe-notification-service`), then blacklists the address, revokes all of that user's OpenIddict tokens, and deletes the account — treat this consumer as idempotent-by-design (it checks existing state before acting, since RabbitMQ redelivery is expected).

### Event-driven side effects

Commands that change account state publish events via `IPublishEndpoint` (MassTransit/RabbitMQ) rather than calling other services directly. Notably, any account deletion (user-initiated, admin, or system/lifecycle-job driven) publishes a deletion event that `recipe-core-api` and others consume to cascade-delete owned domain data (recipes, meal plans, etc.) — when changing deletion handlers, preserve the event publish or downstream cascade deletes silently stop happening.

### `AccountLifecycleJob` (Quartz, daily at 03:00 by default via `AccountLifecycle:CronSchedule`)

Two independent timelines, both skipping users in the `Admin` role, driven by `AccountLifecycleOptions`:
- **Unconfirmed email:** reminder at 7 days → lockout at 14 days → permanent deletion at 30 days.
- **Inactivity** (based on `LastLoginAt ?? CreatedAt`): warning at 6 months → lockout at 1 year → deletion 30 days after that lockout.

Each stage is guarded by a `*SentAt` timestamp field on `ApplicationUser` to stay idempotent across job runs, and each stage publishes a corresponding `Contracts.Events.SystemActions` event.

## Configuration

Strongly-typed `Options` bound from `appsettings.json`/user-secrets/env vars, see `Domain/Options/`: `AppSettings`, `AdminUserOptions`, `JwtOptions`, `AccountLifecycleOptions`, `RabbitMqOptions`. In Docker/production, `:` in config keys becomes `__` in env var names (e.g. `JWT__SecretKey`). Never commit real secrets to `appsettings.json` — the checked-in dev values are for local Docker infra only.

## Documentation

`Documentation/` has more detail than this file on specific subsystems — check it before assuming behavior:
- `01-architecture-and-setup.md` — layering, tech stack, Options/secrets setup
- `02-endpoints-and-controllers.md` — full endpoint reference for all four controllers
- `03-cqrs-and-mediatr.md` — full handler-by-handler catalog
- `04-events-and-messaging.md` — full event catalog and cascade-delete topology
- `05-openiddict-security-and-jobs.md` — token flow detail, Google OAuth flow, lifecycle job detail
- `06-test-strategy.md` — intended test strategy (see Tests note above — not yet implemented)
