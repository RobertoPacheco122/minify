# Minify

A URL shortening service built with .NET 10, PostgreSQL, Redis, and RabbitMQ. It generates short, collision-free codes from long URLs, supports expiration, serves redirect lookups with low latency via a cache-aside strategy, and asynchronously tracks click analytics (device, browser, traffic source, and geolocation) without adding latency to the redirect path.

---

## Features

- Shorten any valid HTTP/HTTPS URL with an optional expiration date
- Redirect short codes to their original URL
- Automatic expiration — expired links return HTTP 410 Gone
- Redis-backed cache layer for fast repeat lookups
- Asynchronous click analytics: device type, browser, OS, traffic source, UTM parameters, and IP-based geolocation, published over RabbitMQ and processed off the request path
- Analytics reporting endpoints — aggregated summaries and paged raw access logs per short code
- Migrations applied automatically on startup

---

## API

### Shorten a URL

```http
POST /api/shorten-url
Content-Type: application/json

{
  "url": "https://example.com/some/very/long/path",
  "expiresAt": "2026-12-31T23:59:59Z"
}
```

**Response `201 Created`**
```json
{
  "isSuccess": true,
  "data": {
    "shortenCode": "x4Kz",
    "expiresAt": "2026-12-31T23:59:59Z"
  },
  "errors": []
}
```

### Resolve a short code

```http
GET /{shortenedCode}
```

- **302** — redirects to the original URL (also fires an async click-tracking event; see [Click analytics](#click-analytics))
- **410** — link has expired
- **404** — code not found
- **400** — invalid input

### Analytics summary

```http
GET /api/analytics/{shortCode}/summary?from={DateTime}&to={DateTime}
```

Returns aggregated click counts for the given short code and date range: total clicks, and breakdowns by country, device type, browser, traffic source, and day.

### Analytics accesses (raw log)

```http
GET /api/analytics/{shortCode}/accesses?from={DateTime}&to={DateTime}&page=1&pageSize=50
```

Returns a paged list of individual access events (most recent first), each with `accessedAt`, `country`, `city`, `deviceType`, `browser`, `operatingSystem`, `trafficSource`, and UTM fields. Raw IP address and referer are stored but not exposed in this response.

---

## Running locally

The easiest way is Docker Compose, which starts the API, PostgreSQL, Redis, and RabbitMQ together:

```bash
docker compose up --build
```

- API: `http://localhost:8080`
- RabbitMQ management UI: `http://localhost:15672` (default credentials `minify` / `minify`)

To run without Docker, set the following environment variables (or add them to `appsettings.Development.json`) and then run:

```bash
dotnet run --project src/Minify.API
```

| Variable | Example value |
|----------|---------------|
| `ConnectionStrings__Postgres` | `Host=localhost;Port=5432;Database=minify;Username=minify;Password=minify` |
| `ConnectionStrings__Redis` | `localhost:6379` |
| `ConnectionStrings__RabbitMQ` | `amqp://minify:minify@localhost:5672` |
| `Hashids__Salt` | any long random string |
| `Hashids__MinLength` | `4` |
| `Geolocation__ApiUrl` | `http://ip-api.com/` |
| `Geolocation__TimeoutMs` | `500` |

You need PostgreSQL, Redis, **and** RabbitMQ running locally for the app to start — RabbitMQ is required even if you don't care about analytics, since MassTransit's bus is wired up at startup. Database migrations are applied automatically when the application starts.

---

## Tests

The project has four test-related projects, using xUnit and FluentAssertions:

| Project | What it covers |
|---------|----------------|
| `CommonTestUtilities` | Shared entity builders and mocks (Moq) used by the other test projects — not a test project itself |
| `UseCases.Test` | Unit tests for use cases — all dependencies mocked, including `TrackUrlClickUseCase` (publisher mocked) |
| `Validators.Test` | Unit tests for FluentValidation validators in isolation |
| `WebApi.Test` | Integration tests for HTTP endpoints — spins up real Postgres and Redis via Testcontainers |

### Running all tests

```bash
dotnet test
```

### Running a specific project

```bash
dotnet test tests/UseCases.Test
dotnet test tests/Validators.Test
dotnet test tests/WebApi.Test
```

### Integration tests (WebApi.Test)

`WebApi.Test` uses [Testcontainers](https://dotnet.testcontainers.org/) to start throwaway Postgres and Redis containers automatically — no local database or Redis instance required. Docker must be running.

`MinifyApiFixture` (`IAsyncLifetime`) handles the full lifecycle:

1. Starts both containers in parallel.
2. Wires them into a `CustomWebApplicationFactory` (replacing the production connection strings and removing all `IHostedService` registrations).
3. Applies EF migrations and seeds the Redis short-code counter.
4. Tears everything down after the test class completes.

**Known gap:** removing `IHostedService` registrations also disables MassTransit's bus (its control services are hosted services too), and no RabbitMQ container is started for this test project. As a result, the publish → consume analytics pipeline (`TrackUrlClickUseCase` → RabbitMQ → `UrlClickedEventConsumer`) is currently covered only at the unit level (`UseCases.Test`, with a mocked publisher), not end-to-end.

---

## Architecture

The project follows Clean Architecture, organized into layers with strict dependency rules (outer layers depend on inner ones, never the reverse):

```
Minify.API              → HTTP layer (Minimal API endpoints)
Minify.Application      → Use cases, interfaces, validation
Minify.Infrastructure   → EF Core, Redis, MassTransit/RabbitMQ, geolocation, repository implementations
Minify.Communication    → Shared DTOs and result wrapper
Minify.Domain           → Entities and repository contracts
Minify.Messaging        → Cross-cutting event contract (UrlClickedEvent, IEventPublisher) — no dependencies of its own
```

`Minify.Messaging` exists so the event contract can be shared between `Application` (which publishes) and `Infrastructure` (which also publishes, and consumes) without creating a dependency cycle or forcing the contract into either layer.

### Request flow — shorten & redirect

```
HTTP Request
    │
    ▼
UrlEndpoints (Minify.API)
    │  maps request/response DTOs
    ▼
Use Case (Minify.Application)
    │  validates input with FluentValidation
    │  orchestrates domain logic
    ▼
Repository / Cache Service (Minify.Infrastructure)
    │
    ├─► Redis (cache-aside)
    └─► PostgreSQL (source of truth)
```

### Click analytics flow (async, off the request path)

```
GET /{shortenedCode}  →  302 redirect returned to the client immediately
    │
    ▼ (awaited, but capped at 100ms and never fails the request)
TrackUrlClickUseCase (Minify.Application)
    │  builds UrlClickedEvent from raw HTTP data (IP, User-Agent, Referer, utm_*)
    ▼
IEventPublisher → RabbitMqEventPublisher (Minify.Infrastructure)
    │  MassTransit publish, "url-clicked" queue, retries at 5s/15s/45s
    ▼
UrlClickedEventConsumer (Minify.Infrastructure)
    │  UAParser → device type / browser / OS
    │  TrafficSourceDetector → utm_source / referer domain / direct / organic
    │  GeolocationService → ip-api.com (soft-fails to null country/city)
    ▼
UrlAccessEventEntity → PostgreSQL (UrlAccessEvents table)
```

If RabbitMQ is unreachable or the publish takes longer than 100ms, `TrackUrlClickUseCase` logs the error and the redirect still succeeds — click tracking is strictly best-effort and never adds latency or failure risk to the hot path. Likewise, if the geolocation lookup fails or times out (`Geolocation:TimeoutMs`, default 500ms), the access event is still stored with `country`/`city` left `null`.

### Short code generation

Each short code is produced by atomically incrementing a counter in Redis (`INCR`) and encoding the resulting integer with [Hashids](https://hashids.org/). This guarantees:

- **No collisions** — every increment produces a unique code.
- **Non-sequential output** — Hashids scrambles the integer using a secret salt, so codes like `x4Kz` reveal nothing about the order or total count.
- **Configurable length** — `Hashids:MinLength` sets the minimum code length. On first startup, `ShortCodeCounterInitializer` seeds the Redis counter to the lowest integer that already satisfies the minimum length, so even the very first code meets the requirement.

### Cache-aside for lookups

When a short code is resolved:

1. Check Redis for a cached entry. If found, redirect immediately.
2. On a cache miss, query PostgreSQL.
3. If the URL exists and is not expired, write it back to Redis (TTL = original `ExpiresAt`) and redirect.
4. If expired, return 410 without caching.

This means only the first lookup for any given code hits the database. All subsequent requests are served from Redis. This path is completely independent of click analytics — analytics is triggered by the endpoint only after a successful resolution, never inside the lookup use case itself.

---

## Technology choices

| Technology | Why |
|------------|-----|
| **.NET 10 / ASP.NET Core Minimal API** | Minimal API removes controller boilerplate while staying fully within the .NET ecosystem. Endpoint registration is explicit and grouped by feature. |
| **PostgreSQL** | Reliable relational store for short URL records and click history. `ShortenCode` is the primary key for `ShortUrls`, making redirect lookups a single indexed read; `UrlAccessEvents` is indexed on `(ShortCode, AccessedAt)`, `(ShortCode, Country)`, and `(ShortCode, TrafficSource)` to keep analytics queries fast. |
| **Entity Framework Core + Npgsql** | Provides type-safe queries and migration management without raw SQL overhead. Migrations run automatically via a hosted service, removing the need for a separate deployment step. |
| **Redis** | Two roles: atomic counter for code generation (`INCR` is a single-node operation with no locking), and cache store for URL lookups. Using Redis for both avoids introducing a second in-memory dependency. |
| **Hashids** | Turns an auto-increment integer into a short, URL-safe, non-guessable string. The salt makes the encoding unique to this deployment, preventing enumeration of codes from other Hashids-based services. |
| **RabbitMQ + MassTransit** | Decouples click tracking from the redirect request so analytics processing (parsing, geolocation, writing to Postgres) never adds latency or a failure mode to the hot redirect path. MassTransit gives retry policies (5s/15s/45s) and a consistent publish/consume abstraction over RabbitMQ without hand-rolling connection and channel management. |
| **UAParser** | Parses `User-Agent` strings into device type, browser, and OS without maintaining a custom parsing table. |
| **ip-api.com (geolocation)** | Free, no-signup IP geolocation lookup, called with a short timeout (`Geolocation:TimeoutMs`, default 500ms) and treated as best-effort — a slow or failed lookup never blocks or fails analytics processing, it just leaves country/city unset. |
| **FluentValidation** | Keeps validation rules out of use cases and easy to unit test in isolation. |
| **Clean Architecture** | Separates business logic from infrastructure concerns. The `Application` layer can be tested without a real database, Redis, or RabbitMQ instance; infrastructure implementations are swappable. `Minify.Messaging` isolates the event contract so it can be shared without coupling `Application` to MassTransit. |
