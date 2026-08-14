# Analytics Feature Design

**Date:** 2026-05-02  
**Status:** Approved  
**Author:** Roberto Pacheco

---

## Overview

Add click analytics to Minify with zero impact on redirect performance. Every URL redirect publishes a lightweight event to RabbitMQ. A dedicated consumer processes the event asynchronously — parsing User-Agent, detecting traffic source, calling a geolocation API — and writes the result to Postgres. Clients query analytics via two REST endpoints.

---

## Architecture

### New Project: `Minify.Messaging`

Houses messaging contracts, following the same rationale as `Minify.Communication` for HTTP contracts.

**Contents:**
- `UrlClickedEvent` — event payload (record)
- `IEventPublisher` — publisher interface

**Dependency graph:**
```
Minify.API            → Minify.Messaging  (injects IEventPublisher in endpoint)
Minify.Infrastructure → Minify.Messaging  (implements IEventPublisher + consumer)
```

`Minify.Application` does not reference `Minify.Messaging` — the publisher is injected at the endpoint level, keeping use cases free of messaging dependencies.

### Changes per Layer

| Layer | Changes |
|---|---|
| `Minify.Messaging` | New project: `UrlClickedEvent`, `IEventPublisher` |
| `Minify.Domain` | `UrlAccessEventEntity`, `IUrlAccessEventWriteOnlyRepository` |
| `Minify.Infrastructure` | EF config, repository impl, `RabbitMqEventPublisher`, `UrlClickedEventConsumer` |
| `Minify.Application` | New use cases: `GetAnalyticsSummaryUseCase`, `GetAnalyticsAccessesUseCase` |
| `Minify.Communication` | New response DTOs for analytics |
| `Minify.API` | `AnalyticsEndpoints.cs`, DI registration |

### Libraries

| Library | Purpose |
|---|---|
| `MassTransit` + `MassTransit.RabbitMQ` | Message broker abstraction, retry, DLQ, consumer lifecycle |
| `UAParser` (ua-parser-csharp) | User-Agent parsing — browser, OS, device type. No network I/O. |
| **ip-api.com** (free tier) | IP → country + city. Called async inside consumer. Free tier: 45 req/min. For higher volume, upgrade to pro or self-host MaxMind GeoLite2. |

---

## Event Flow

### Publish (redirect hot path)

```
GET /{shortenedCode}
  → RetrieveOriginalUrlUseCase.Execute()
  → result.Status == Found
  → IEventPublisher.PublishAsync(UrlClickedEvent)   ← fire-and-forget
  → Results.Redirect(longUrl)                        ← immediate response to user
```

Publishing happens in `UrlEndpoints.cs`, not inside the use case. This keeps the use case as pure domain logic with no messaging dependency.

`PublishAsync` is called with `await` but wrapped in a `try/catch` with a short timeout (100 ms). If RabbitMQ is unavailable, the exception is caught, logged, and the redirect proceeds normally. Analytics never blocks the user.

### Event Payload (`UrlClickedEvent`)

```csharp
public record UrlClickedEvent
{
    public string ShortCode { get; init; }
    public DateTime Timestamp { get; init; }       // UTC
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? Referer { get; init; }
    public string? UtmSource { get; init; }
    public string? UtmMedium { get; init; }
    public string? UtmCampaign { get; init; }
}
```

All fields are captured from `HttpContext` at the endpoint before redirecting.

### RabbitMQ Topology (managed by MassTransit)

| Component | Name |
|---|---|
| Exchange | `minify.url-clicked` |
| Queue | `url-clicked` |
| Dead Letter Queue | `url-clicked_error` (auto-created by MassTransit) |
| Durability | Durable queue, persistent messages |
| Retry policy | 3 attempts: 5 s → 15 s → 45 s, then DLQ |

### Consumer Processing (`UrlClickedEventConsumer`)

For each event received from the queue:

1. **User-Agent parsing** — `UAParser` extracts `Browser`, `OperatingSystem`, `DeviceType`. No I/O.
2. **Traffic source detection:**
   - `UtmSource` present → use it as `TrafficSource`
   - Else: map `Referer` host against a known-domain table:
     - `facebook.com`, `l.facebook.com`, `m.facebook.com` → `facebook`
     - `wa.me`, `web.whatsapp.com` → `whatsapp`
     - `t.co`, `twitter.com`, `x.com` → `twitter`
     - `instagram.com`, `l.instagram.com` → `instagram`
     - `mail.google.com`, `outlook.live.com`, `outlook.com`, `yahoo.com` → `email`
     - Any other non-empty Referer with a recognizable host → `organic`
   - Else (no Referer, no UTM): `"direct"`
3. **Geolocation** — HTTP call to external API (e.g., ip-api.com) with `IpAddress`. Returns `Country` + `City`. If the call fails or times out, fields are left `null` — partial data is preferable to event loss.
4. **Persist** — write `UrlAccessEventEntity` to Postgres via `IUrlAccessEventWriteOnlyRepository`.

---

## Data Model

### `UrlAccessEventEntity`

| Column | Type | Nullable | Description |
|---|---|---|---|
| `Id` | `Guid` | no | Primary key |
| `ShortCode` | `string` | no | FK → `ShortUrls.ShortenCode` |
| `AccessedAt` | `DateTime` (UTC) | no | Click timestamp |
| `IpAddress` | `string` | yes | Visitor IP |
| `Country` | `string` | yes | e.g. `Brazil` |
| `City` | `string` | yes | e.g. `São Paulo` |
| `DeviceType` | `string` | no | `Desktop`, `Mobile`, `Tablet` |
| `Browser` | `string` | no | `Chrome`, `Firefox`, `Safari`, `Unknown` |
| `OperatingSystem` | `string` | no | `Windows`, `Android`, `iOS`, `Unknown` |
| `TrafficSource` | `string` | no | `facebook`, `whatsapp`, `email`, `direct`, `organic`, … |
| `UtmSource` | `string` | yes | Raw `utm_source` value |
| `UtmMedium` | `string` | yes | Raw `utm_medium` value |
| `UtmCampaign` | `string` | yes | Raw `utm_campaign` value |
| `Referer` | `string` | yes | Raw `Referer` header |

**FK constraint:** `ShortCode` references `ShortUrls` without `ON DELETE CASCADE` — historical analytics are preserved if a short URL is deleted.

**Indexes:**
- `(ShortCode, AccessedAt)` — primary access pattern for period queries
- `(ShortCode, Country)` — geographic filter
- `(ShortCode, TrafficSource)` — source filter

---

## Analytics API Endpoints

Registered in `AnalyticsEndpoints.cs` following the existing `MapUrlEndpoints` pattern.

### `GET /api/analytics/{shortCode}/summary`

Returns aggregated totals for a period.

**Query params:** `from` (ISO 8601, required), `to` (ISO 8601, required)

**Response `200 OK`:**
```json
{
  "totalClicks": 1240,
  "byCountry":  [{ "country": "Brazil", "clicks": 980 }],
  "byDevice":   [{ "deviceType": "Mobile", "clicks": 850 }],
  "byBrowser":  [{ "browser": "Chrome", "clicks": 700 }],
  "bySource":   [{ "source": "whatsapp", "clicks": 520 }],
  "byDay":      [{ "date": "2026-05-01", "clicks": 340 }]
}
```

### `GET /api/analytics/{shortCode}/accesses`

Paginated list of individual click events.

**Query params:** `from` (required), `to` (required), `page` (default 1), `pageSize` (default 50, max 200)

**Response `200 OK`:**
```json
{
  "page": 1,
  "pageSize": 50,
  "total": 1240,
  "items": [
    {
      "accessedAt": "2026-05-01T14:32:00Z",
      "country": "Brazil",
      "city": "São Paulo",
      "deviceType": "Mobile",
      "browser": "Chrome",
      "operatingSystem": "Android",
      "trafficSource": "whatsapp",
      "utmSource": null,
      "utmMedium": null,
      "utmCampaign": null
    }
  ]
}
```

---

## Infrastructure Changes

### `docker-compose.yml`

Add RabbitMQ service:

```yaml
rabbitmq:
  image: rabbitmq:3-management-alpine
  ports:
    - "5672:5672"    # AMQP
    - "15672:15672"  # Management UI
  environment:
    RABBITMQ_DEFAULT_USER: minify
    RABBITMQ_DEFAULT_PASS: minify
  healthcheck:
    test: ["CMD", "rabbitmq-diagnostics", "ping"]
    interval: 10s
    timeout: 5s
    retries: 5
  restart: unless-stopped
```

### `appsettings.json` — new keys

| Key | Description |
|---|---|
| `ConnectionStrings:RabbitMQ` | `amqp://minify:minify@localhost:5672` |
| `Geolocation:ApiUrl` | Base URL of the IP geolocation API |
| `Geolocation:TimeoutMs` | Timeout for geolocation calls (default: `500`) |

---

## Error Handling

| Scenario | Behavior |
|---|---|
| RabbitMQ unavailable on publish | Exception caught, event discarded, redirect proceeds, error logged |
| Geolocation API fails/times out | `Country`/`City` saved as `null`, event persisted normally |
| Consumer throws after 3 retries | Event moved to `url-clicked_error` DLQ for manual inspection |
| `from`/`to` missing in analytics query | `400 Bad Request` via `ResultJson.Failure` |
| `shortCode` not found in analytics query | `200 OK` with `totalClicks: 0` (no events is a valid state) |
