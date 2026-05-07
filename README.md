# Minify

A URL shortening service built with .NET 10, PostgreSQL, and Redis. It generates short, collision-free codes from long URLs, supports expiration, and serves redirect lookups with low latency via a cache-aside strategy.

---

## Features

- Shorten any valid HTTP/HTTPS URL with an optional expiration date
- Redirect short codes to their original URL
- Automatic expiration — expired links return HTTP 410 Gone
- Redis-backed cache layer for fast repeat lookups
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

- **302** — redirects to the original URL
- **410** — link has expired
- **404** — code not found
- **400** — invalid input

---

## Running locally

The easiest way is Docker Compose, which starts the API, PostgreSQL, and Redis together:

```bash
docker compose up --build
```

The API is available at `http://localhost:8080`.

To run without Docker, set the following environment variables (or add them to `appsettings.Development.json`) and then run:

```bash
dotnet run --project src/Minify.API
```

| Variable | Example value |
|----------|---------------|
| `ConnectionStrings__Postgres` | `Host=localhost;Port=5432;Database=minify;Username=minify;Password=minify` |
| `ConnectionStrings__Redis` | `localhost:6379` |
| `Hashids__Salt` | any long random string |
| `Hashids__MinLength` | `4` |

Database migrations are applied automatically when the application starts.

---

## Tests

The project has three test projects, all using xUnit and FluentAssertions:

| Project | What it covers |
|---------|----------------|
| `UseCases.Test` | Unit tests for use cases — all dependencies mocked |
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
2. Wires them into a `CustomWebApplicationFactory` (replacing the production connection strings and removing hosted services).
3. Applies EF migrations and seeds the Redis short-code counter.
4. Tears everything down after the test class completes.

---

## Architecture

The project follows Clean Architecture, organized into four layers with strict dependency rules (outer layers depend on inner ones, never the reverse):

```
Minify.API              → HTTP layer (Minimal API endpoints)
Minify.Application      → Use cases, interfaces, validation
Minify.Infrastructure   → EF Core, Redis, repository implementations
Minify.Communication    → Shared DTOs and result wrapper
Minify.Domain           → Entities and repository contracts
```

### Request flow

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

This means only the first lookup for any given code hits the database. All subsequent requests are served from Redis.

---

## Technology choices

| Technology | Why |
|------------|-----|
| **.NET 10 / ASP.NET Core Minimal API** | Minimal API removes controller boilerplate while staying fully within the .NET ecosystem. Endpoint registration is explicit and grouped by feature. |
| **PostgreSQL** | Reliable relational store for the short URL records. `ShortenCode` is the primary key, making lookups a single indexed read. |
| **Entity Framework Core + Npgsql** | Provides type-safe queries and migration management without raw SQL overhead for a schema this simple. Migrations run automatically via a hosted service, removing the need for a separate deployment step. |
| **Redis** | Two roles: atomic counter for code generation (`INCR` is a single-node operation with no locking), and cache store for URL lookups. Using Redis for both avoids introducing a second in-memory dependency. |
| **Hashids** | Turns an auto-increment integer into a short, URL-safe, non-guessable string. The salt makes the encoding unique to this deployment, preventing enumeration of codes from other Hashids-based services. |
| **FluentValidation** | Keeps validation rules out of use cases and easy to unit test in isolation. |
| **Clean Architecture** | Separates business logic from infrastructure concerns. The `Application` layer can be tested without a real database or Redis instance; infrastructure implementations are swappable. |
