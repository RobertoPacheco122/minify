# CLAUDE.md

## Commands

```bash
dotnet build
dotnet run --project src/Minify.API                          # requires Postgres + Redis + RabbitMQ
docker compose up --build                                    # recommended

dotnet ef migrations add <Name> --project src/Minify.Infrastructure --startup-project src/Minify.API
dotnet ef database update --project src/Minify.Infrastructure --startup-project src/Minify.API

dotnet test
dotnet test tests/UseCases.Test
dotnet test tests/Validators.Test
dotnet test tests/WebApi.Test                                 # needs Docker running (Testcontainers)
```

## Architecture

Clean Architecture: `Domain` → `Application` → `Infrastructure` / `API`. `Communication` holds shared DTOs and `ResultJson<T>`. `Messaging` holds the cross-cutting event contract (`UrlClickedEvent`, `IEventPublisher`) with no project dependencies of its own, so both `Application` (publishes) and `Infrastructure` (publishes/consumes via MassTransit) can reference it without a cycle.

- **Use cases**: validate with FluentValidation → call interfaces → return `ResultJson<T>` (never throw)
- **Endpoints**: minimal API, registered via extension methods in `Endpoints/`, map `ResultJson` to HTTP responses
- **Repositories**: split into read-only / write-only interfaces (`IShortUrlReadOnlyRepository`/`IShortUrlWriteOnlyRepository`, `IUrlAccessEventReadOnlyRepository`/`IUrlAccessEventWriteOnlyRepository`), both scoped

## Non-obvious Design

**Short code**: Redis atomic `INCR` counter encoded with Hashids. `ShortCodeCounterInitializer` (hosted service) seeds the counter on startup to the minimum value that yields `Hashids:MinLength` characters.

**Cache-aside**: lookup checks Redis → Postgres. Expiry is checked in the use case (not the query); expired → `RetrieveOriginalUrlStatus.Expired` → HTTP 410. On DB hit, writes to Redis with `ExpiresAt` as TTL. This flow is entirely untouched by analytics — `RetrieveOriginalUrlUseCase` has no knowledge of click tracking.

**Click analytics (async, best-effort)**: on a successful redirect (`GET /{shortenedCode}`), `UrlEndpoints` gathers raw HTTP data (IP from `X-Forwarded-For`/`RemoteIpAddress`, `User-Agent`, `Referer`, `utm_*` query params) and calls `TrackUrlClickUseCase`, which builds and publishes a `UrlClickedEvent` through `IEventPublisher` (MassTransit → RabbitMQ, `RabbitMqEventPublisher`). The publish is capped at a 100ms linked-token timeout and any failure is caught and logged — a broker outage never breaks the redirect, it only drops that click's analytics. `UrlClickedEventConsumer` handles the message asynchronously: parses `User-Agent` with `UAParser` (device type/browser/OS), resolves `TrafficSource` via `TrafficSourceDetector` (utm_source → referer domain map → `"direct"`/`"organic"`), optionally geolocates the IP via `IGeolocationService` (ip-api.com, fails soft — `null` country/city on any error or timeout), and persists a `UrlAccessEventEntity` row. The RabbitMQ receive endpoint (`"url-clicked"`) retries failed consumes at 5s/15s/45s intervals.

**Analytics reads**: `GetAnalyticsSummaryUseCase`/`GetAnalyticsAccessesUseCase` read `UrlAccessEvents` directly (summary aggregates in-memory by country/device/browser/source/day; accesses is a paged raw list). No caching layer — these are low-traffic admin/reporting queries, not the hot redirect path.

**Startup hosted services**: `DatabaseInitializer` (runs pending EF migrations) and `ShortCodeCounterInitializer` (seeds Redis counter). Integration tests remove all `IHostedService` registrations — this also disables MassTransit's bus (its control services are hosted services too), so `WebApi.Test` currently does not exercise the publish/consume analytics path at all; only Postgres + Redis are spun up via Testcontainers, no RabbitMQ container.

## Configuration

| Key | Description |
|-----|-------------|
| `ConnectionStrings:Postgres` | Npgsql connection string |
| `ConnectionStrings:Redis` | `host:port` |
| `ConnectionStrings:RabbitMQ` | AMQP URI, e.g. `amqp://minify:minify@localhost:5672` |
| `Hashids:Salt` | Secret salt |
| `Hashids:MinLength` | Minimum short code length |
| `Geolocation:ApiUrl` | Base URL for the geolocation lookup (ip-api.com) |
| `Geolocation:TimeoutMs` | HTTP timeout for the geolocation call (default 500ms); on timeout/error, country/city are left `null` |

## Guidelines

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:

- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:

- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:

- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:

- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:

```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

## Workflow Orchestration

### 1. Self-Improvement Loop

- After ANY correction from the user: update `/docs/lessons.md`, in project root, with the pattern
- Write rules for yourself that prevent the same mistake
- Ruthlessly iterate on these lessons until mistake rate drops
- Review lessons at session start for relevant project

### 2. Demand Elegance (Balanced)

- For non-trivial changes: pause and ask "is there a more elegant way?"
- If a fix feels hacky: "Knowing everything I know now, implement the elegant solution"
- Skip this for simple, obvious fixes - don't over-engineer
- Challenge your own work before presenting it
