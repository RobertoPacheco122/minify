# Test Structure Design — Minify

**Date:** 2026-05-02
**Status:** Approved

## Context

The Minify project has no automated tests. This spec defines a 4-project test suite covering use cases, validators, and end-to-end API behavior, including real data persistence via Testcontainers.

**Decisions made:**
- Test framework: xUnit
- Mocking: Moq with named setup methods
- Integration infrastructure: Testcontainers (PostgreSQL + Redis)
- Naming convention: `Should_[Resultado]_When_[Condição]`
- Builder approach: static methods, valid by default, named invalid variations

---

## Project Structure

```
tests/
├── Minify.CommonTestUtilities/
│   ├── Minify.CommonTestUtilities.csproj
│   ├── Builders/
│   │   ├── Entities/ShortUrlEntityBuilder.cs
│   │   └── Requests/RequestShortenUrlJsonBuilder.cs
│   └── Mocks/
│       ├── Repositories/ShortUrlReadOnlyRepositoryMock.cs
│       ├── Repositories/ShortUrlWriteOnlyRepositoryMock.cs
│       ├── Services/UrlCacheServiceMock.cs
│       ├── Services/ShortCodeGeneratorMock.cs
│       └── UnitOfWorkMock.cs
│
├── Minify.UseCases.Test/
│   ├── Minify.UseCases.Test.csproj
│   └── UseCases/Url/
│       ├── ShortenUrl/ShortenUrlUseCaseTests.cs
│       └── RetrieveOriginalUrl/RetrieveOriginalUrlUseCaseTests.cs
│
├── Minify.Validators.Test/
│   ├── Minify.Validators.Test.csproj
│   └── Validators/Url/
│       ├── ShortenUrlValidatorTests.cs
│       └── RetrieveOriginalUrlValidatorTests.cs
│
└── Minify.WebApi.Test/
    ├── Minify.WebApi.Test.csproj
    ├── Infrastructure/
    │   ├── CustomWebApplicationFactory.cs
    │   └── MinifyApiFixture.cs
    └── Controllers/Url/
        ├── ShortenUrlTests.cs
        └── RetrieveOriginalUrlTests.cs
```

---

## Project 1: Minify.CommonTestUtilities

Shared utilities referenced by all test projects. No test runner — pure helper library.

### Packages
- `Moq`
- Project references: `Minify.Application`, `Minify.Domain`, `Minify.Communication`

### Builders

Builders produce valid objects by default. Named variations produce invalid inputs for error scenarios.

**`ShortUrlEntityBuilder`**
- `Build(string? longUrl = null, DateTime? expiresAt = null)` — valid entity, `ExpiresAt = UtcNow + 1 day`
- `BuildExpired()` — `ExpiresAt = UtcNow - 1 day`

**`RequestShortenUrlJsonBuilder`**
- `Build()` — `Url = "https://example.com"`, `ExpiresAt = UtcNow + 1 day`
- `BuildWithInvalidUrl()` — `Url = "not-a-url"`
- `BuildWithPastExpiration()` — `ExpiresAt = UtcNow - 1 day`

### Mocks

Each mock class wraps a `Mock<T>` and exposes named setup methods. Tests inject `.Mock.Object`.

**`ShortUrlReadOnlyRepositoryMock`**
- `SetupFound(ShortUrlEntity entity)` — returns entity for any short code
- `SetupNotFound()` — returns null for any short code

**`ShortUrlWriteOnlyRepositoryMock`**
- No setup methods needed; used to verify `Add` was called once

**`UrlCacheServiceMock`**
- `SetupFound(CachedUrl cachedUrl)` — `Get` returns the cached URL
- `SetupNotFound()` — `Get` returns null

**`ShortCodeGeneratorMock`**
- `Setup(string code = "abcd")` — `Generate` returns a fixed code

**`UnitOfWorkMock`**
- No setup needed; used to verify `Commit` was called once

---

## Project 2: Minify.UseCases.Test

Tests use case behavior directly, with all dependencies mocked.

### Packages
- `xunit`, `xunit.runner.visualstudio`, `FluentAssertions`, `coverlet.collector`
- Project references: `Minify.CommonTestUtilities`, `Minify.Application`

### ShortenUrlUseCaseTests

| Test | Scenario | Assertion |
|------|----------|-----------|
| `Should_ReturnSuccess_When_RequestIsValid` | Valid request | `IsSuccess = true`, response has `ShortenCode`, `Commit` called once |
| `Should_ReturnError_When_UrlIsInvalid` | Relative URL | `IsSuccess = false`, errors contain URL field |
| `Should_ReturnError_When_ExpiresAtIsInThePast` | Past date | `IsSuccess = false`, errors contain ExpiresAt field |

### RetrieveOriginalUrlUseCaseTests

| Test | Scenario | Assertion |
|------|----------|-----------|
| `Should_ReturnFound_When_UrlExistsInCache` | Cache hit | `Status = Found`, repository `GetByShortCode` never called |
| `Should_ReturnFound_When_UrlExistsInDatabase` | Cache miss, DB hit | `Status = Found`, cache `Set` called once |
| `Should_ReturnNotFound_When_UrlDoesNotExist` | Cache miss, DB miss | `Status = NotFound` |
| `Should_ReturnExpired_When_UrlIsExpired` | DB returns expired entity | `Status = Expired` |

---

## Project 3: Minify.Validators.Test

Tests FluentValidation validators in isolation. No mocks required — validators are instantiated directly.

### Packages
- `xunit`, `xunit.runner.visualstudio`, `FluentAssertions`
- Project references: `Minify.CommonTestUtilities`, `Minify.Application`

### ShortenUrlValidatorTests

| Test | Input | Expected |
|------|-------|----------|
| `Should_Pass_When_RequestIsValid` | `Build()` | Valid |
| `Should_Fail_When_UrlIsEmpty` | `Url = ""` | Invalid, Url field |
| `Should_Fail_When_UrlIsRelative` | `Url = "/path/only"` | Invalid, Url field |
| `Should_Fail_When_UrlSchemeIsNotHttpOrHttps` | `Url = "ftp://x.com"` | Invalid, Url field |
| `Should_Fail_When_ExpiresAtIsInThePast` | `ExpiresAt = UtcNow - 1s` | Invalid, ExpiresAt field |
| `Should_Fail_When_ExpiresAtIsNow` | `ExpiresAt = UtcNow` | Invalid, ExpiresAt field (boundary) |

### RetrieveOriginalUrlValidatorTests

| Test | Input | Expected |
|------|-------|----------|
| `Should_Pass_When_CodeIsValid` | `"abcd"` | Valid |
| `Should_Fail_When_CodeIsEmpty` | `""` | Invalid |
| `Should_Fail_When_CodeIsTooShort` | `"abc"` (3 chars) | Invalid |
| `Should_Fail_When_CodeIsTooLong` | `"abcdefgh"` (8 chars) | Invalid |
| `Should_Fail_When_CodeHasSpecialCharacters` | `"ab!cd"` | Invalid |

---

## Project 4: Minify.WebApi.Test

End-to-end tests against a real API with real PostgreSQL and Redis via Testcontainers.

### Packages
- `xunit`, `xunit.runner.visualstudio`, `FluentAssertions`
- `Microsoft.AspNetCore.Mvc.Testing`
- `Testcontainers.PostgreSql`, `Testcontainers.Redis`
- Project references: `Minify.CommonTestUtilities`, `Minify.API`

### Infrastructure

**`CustomWebApplicationFactory : WebApplicationFactory<Program>`**

Overrides `ConfigureWebHost` to:
- Replace `ConnectionStrings:Postgres` and `ConnectionStrings:Redis` with Testcontainers connection strings
- Remove `DatabaseInitializer` and `ShortCodeCounterInitializer` hosted services (factory handles this manually)

**`MinifyApiFixture : IAsyncLifetime`**

`InitializeAsync`:
1. Start `PostgreSqlContainer` and `RedisContainer` via Testcontainers
2. Instantiate `CustomWebApplicationFactory` with container connection strings
3. Apply EF Core migrations via `IServiceScope` → `MinifyDbContext.Database.MigrateAsync()`
4. Initialize Redis counter via `IConnectionMultiplexer` (same logic as `ShortCodeCounterInitializer`)
5. Create `HttpClient` via `factory.CreateClient(new() { AllowAutoRedirect = false })`

`DisposeAsync`: dispose factory, stop and dispose containers.

Test classes implement `IClassFixture<MinifyApiFixture>` — containers start once per class.

### ShortenUrlTests

| Test | Request | Assertion |
|------|---------|-----------|
| `Should_Return201_When_RequestIsValid` | Valid body | Status 201, body has `shortenCode` and `expiresAt`, row exists in DB |
| `Should_Return400_When_UrlIsInvalid` | Invalid URL | Status 400 |
| `Should_Return400_When_ExpiresAtIsInThePast` | Past date | Status 400 |

### RetrieveOriginalUrlTests

| Test | Setup | Assertion |
|------|-------|-----------|
| `Should_Return302_When_CodeExists` | POST to create URL, then GET by code | Status 302, `Location` header = original URL |
| `Should_Return404_When_CodeDoesNotExist` | GET with random non-existent code | Status 404 |
| `Should_Return410_When_UrlIsExpired` | Insert expired row directly in DB, GET by code | Status 410 |

**End-to-end redirect flow:**
```
POST /api/shorten-url → extract shortenCode from response body
GET /{shortenCode}    → assert 302 + Location: <original URL>
```

`HttpClient` must be created with `AllowAutoRedirect = false` so the test can assert the 302 directly.

---

## What is NOT in scope

- Infrastructure unit tests (Redis/EF implementations) — covered by WebApi.Test end-to-end
- Performance or load tests
- Authentication/authorization (not implemented)
