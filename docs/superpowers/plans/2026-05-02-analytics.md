# Analytics Feature Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add click analytics to Minify — every redirect publishes a lightweight event to RabbitMQ; a consumer processes it asynchronously and stores device, browser, geolocation, and traffic source data per click; two REST endpoints expose the data to clients.

**Architecture:** Fire-and-forget publish from the redirect endpoint into a durable RabbitMQ queue. A MassTransit consumer handles User-Agent parsing (UAParser), traffic source detection, and geolocation (ip-api.com) before persisting to a new `UrlAccessEvents` Postgres table. Query use cases aggregate from that table and are exposed via REST endpoints.

**Tech Stack:** MassTransit + RabbitMQ transport, UAParser (ua-parser-csharp), ip-api.com (HTTP), EF Core 10 (Npgsql), FluentValidation 12.

---

## File Map

### New files
```
src/Minify.Messaging/
  Minify.Messaging.csproj
  Events/UrlClickedEvent.cs
  Publishers/IEventPublisher.cs

src/Minify.Domain/
  Entities/UrlAccessEventEntity.cs
  Repositories/UrlAccessEvent/IUrlAccessEventWriteOnlyRepository.cs
  Repositories/UrlAccessEvent/IUrlAccessEventReadOnlyRepository.cs

src/Minify.Communication/
  Responses/Analytics/ResponseAnalyticsSummaryJson.cs
  Responses/Analytics/ResponseAnalyticsAccessesJson.cs

src/Minify.Application/
  Services/Geolocation/IGeolocationService.cs
  UseCases/Analytics/GetSummary/IGetAnalyticsSummaryUseCase.cs
  UseCases/Analytics/GetSummary/GetAnalyticsSummaryValidator.cs
  UseCases/Analytics/GetSummary/GetAnalyticsSummaryUseCase.cs
  UseCases/Analytics/GetAccesses/IGetAnalyticsAccessesUseCase.cs
  UseCases/Analytics/GetAccesses/GetAnalyticsAccessesValidator.cs
  UseCases/Analytics/GetAccesses/GetAnalyticsAccessesUseCase.cs

src/Minify.Infrastructure/
  DataAccess/Configurations/UrlAccessEventEntityConfiguration.cs
  DataAccess/Repositories/UrlAccessEvent/UrlAccessEventWriteOnlyRepository.cs
  DataAccess/Repositories/UrlAccessEvent/UrlAccessEventReadOnlyRepository.cs
  Services/Geolocation/GeolocationService.cs
  Services/Analytics/TrafficSourceDetector.cs
  Services/Messaging/RabbitMqEventPublisher.cs
  Services/Messaging/UrlClickedEventConsumer.cs

src/Minify.API/
  Endpoints/AnalyticsEndpoints.cs
```

### Modified files
```
src/Minify.Infrastructure/DataAccess/MinifyDbContext.cs         — add UrlAccessEvents DbSet + config
src/Minify.Infrastructure/DependencyInjection.cs                — register MassTransit, repos, geolocation
src/Minify.Application/DependencyInjection.cs                   — register analytics use cases
src/Minify.API/Program.cs                                       — call MapAnalyticsEndpoints
src/Minify.API/Endpoints/UrlEndpoints.cs                        — inject IEventPublisher + publish on redirect
src/Minify.API/Minify.API.csproj                                — add Minify.Messaging reference
src/Minify.Infrastructure/Minify.Infrastructure.csproj          — add Minify.Messaging reference + NuGet packages
docker-compose.yml                                              — add RabbitMQ service
src/Minify.API/appsettings.json                                 — add RabbitMQ + Geolocation keys
src/Minify.API/appsettings.Development.json                     — add dev connection strings
```

---

## Task 1: Create `Minify.Messaging` project

**Files:**
- Create: `src/Minify.Messaging/Minify.Messaging.csproj`
- Create: `src/Minify.Messaging/Events/UrlClickedEvent.cs`
- Create: `src/Minify.Messaging/Publishers/IEventPublisher.cs`
- Modify: `src/Minify.Infrastructure/Minify.Infrastructure.csproj`
- Modify: `src/Minify.API/Minify.API.csproj`

- [ ] **Step 1: Scaffold the project and add it to the solution**

```bash
cd /path/to/Minify
dotnet new classlib -n Minify.Messaging -o src/Minify.Messaging
dotnet sln add src/Minify.Messaging/Minify.Messaging.csproj
rm src/Minify.Messaging/Class1.cs
```

- [ ] **Step 2: Align the `.csproj` with the rest of the solution**

Replace the contents of `src/Minify.Messaging/Minify.Messaging.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

</Project>
```

- [ ] **Step 3: Create `UrlClickedEvent.cs`**

Create `src/Minify.Messaging/Events/UrlClickedEvent.cs`:

```csharp
namespace Minify.Messaging.Events;

public record UrlClickedEvent
{
    public string ShortCode { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? Referer { get; init; }
    public string? UtmSource { get; init; }
    public string? UtmMedium { get; init; }
    public string? UtmCampaign { get; init; }
}
```

- [ ] **Step 4: Create `IEventPublisher.cs`**

Create `src/Minify.Messaging/Publishers/IEventPublisher.cs`:

```csharp
namespace Minify.Messaging.Publishers;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
}
```

- [ ] **Step 5: Add project references**

```bash
dotnet add src/Minify.Infrastructure/Minify.Infrastructure.csproj reference src/Minify.Messaging/Minify.Messaging.csproj
dotnet add src/Minify.API/Minify.API.csproj reference src/Minify.Messaging/Minify.Messaging.csproj
```

- [ ] **Step 6: Build and verify**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 7: Commit**

```bash
git add src/Minify.Messaging/ src/Minify.Infrastructure/Minify.Infrastructure.csproj src/Minify.API/Minify.API.csproj Minify.sln
git commit -m "feat: add Minify.Messaging project with UrlClickedEvent and IEventPublisher"
```

---

## Task 2: Domain — `UrlAccessEventEntity` + repository interfaces

**Files:**
- Create: `src/Minify.Domain/Entities/UrlAccessEventEntity.cs`
- Create: `src/Minify.Domain/Repositories/UrlAccessEvent/IUrlAccessEventWriteOnlyRepository.cs`
- Create: `src/Minify.Domain/Repositories/UrlAccessEvent/IUrlAccessEventReadOnlyRepository.cs`

- [ ] **Step 1: Create `UrlAccessEventEntity.cs`**

Create `src/Minify.Domain/Entities/UrlAccessEventEntity.cs`:

```csharp
namespace Minify.Domain.Entities;

public class UrlAccessEventEntity
{
    public Guid Id { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public DateTime AccessedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string TrafficSource { get; set; } = string.Empty;
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? Referer { get; set; }
}
```

- [ ] **Step 2: Create `IUrlAccessEventWriteOnlyRepository.cs`**

Create `src/Minify.Domain/Repositories/UrlAccessEvent/IUrlAccessEventWriteOnlyRepository.cs`:

```csharp
using Minify.Domain.Entities;

namespace Minify.Domain.Repositories.UrlAccessEvent;

public interface IUrlAccessEventWriteOnlyRepository
{
    Task Add(UrlAccessEventEntity entity, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Create `IUrlAccessEventReadOnlyRepository.cs`**

Create `src/Minify.Domain/Repositories/UrlAccessEvent/IUrlAccessEventReadOnlyRepository.cs`:

```csharp
using Minify.Domain.Entities;

namespace Minify.Domain.Repositories.UrlAccessEvent;

public interface IUrlAccessEventReadOnlyRepository
{
    Task<List<UrlAccessEventEntity>> GetAll(
        string shortCode,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<(List<UrlAccessEventEntity> Items, int Total)> GetPaged(
        string shortCode,
        DateTime from,
        DateTime to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Build and verify**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/Minify.Domain/
git commit -m "feat: add UrlAccessEventEntity and analytics repository interfaces"
```

---

## Task 3: Communication — analytics response DTOs

**Files:**
- Create: `src/Minify.Communication/Responses/Analytics/ResponseAnalyticsSummaryJson.cs`
- Create: `src/Minify.Communication/Responses/Analytics/ResponseAnalyticsAccessesJson.cs`

- [ ] **Step 1: Create `ResponseAnalyticsSummaryJson.cs`**

Create `src/Minify.Communication/Responses/Analytics/ResponseAnalyticsSummaryJson.cs`:

```csharp
namespace Minify.Communication.Responses.Analytics;

public class ResponseAnalyticsSummaryJson
{
    public int TotalClicks { get; set; }
    public List<CountryClicksJson> ByCountry { get; set; } = [];
    public List<DeviceClicksJson> ByDevice { get; set; } = [];
    public List<BrowserClicksJson> ByBrowser { get; set; } = [];
    public List<SourceClicksJson> BySource { get; set; } = [];
    public List<DayClicksJson> ByDay { get; set; } = [];
}

public class CountryClicksJson
{
    public string Country { get; set; } = string.Empty;
    public int Clicks { get; set; }
}

public class DeviceClicksJson
{
    public string DeviceType { get; set; } = string.Empty;
    public int Clicks { get; set; }
}

public class BrowserClicksJson
{
    public string Browser { get; set; } = string.Empty;
    public int Clicks { get; set; }
}

public class SourceClicksJson
{
    public string Source { get; set; } = string.Empty;
    public int Clicks { get; set; }
}

public class DayClicksJson
{
    public string Date { get; set; } = string.Empty;
    public int Clicks { get; set; }
}
```

- [ ] **Step 2: Create `ResponseAnalyticsAccessesJson.cs`**

Create `src/Minify.Communication/Responses/Analytics/ResponseAnalyticsAccessesJson.cs`:

```csharp
namespace Minify.Communication.Responses.Analytics;

public class ResponseAnalyticsAccessesJson
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AccessEventItemJson> Items { get; set; } = [];
}

public class AccessEventItemJson
{
    public DateTime AccessedAt { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string TrafficSource { get; set; } = string.Empty;
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/Minify.Communication/
git commit -m "feat: add analytics response DTOs"
```

---

## Task 4: Application — `IGeolocationService`

**Files:**
- Create: `src/Minify.Application/Services/Geolocation/IGeolocationService.cs`

- [ ] **Step 1: Create `IGeolocationService.cs`**

Create `src/Minify.Application/Services/Geolocation/IGeolocationService.cs`:

```csharp
namespace Minify.Application.Services.Geolocation;

public interface IGeolocationService
{
    Task<GeolocationResult?> GetAsync(string ipAddress, CancellationToken cancellationToken = default);
}

public record GeolocationResult(string Country, string City);
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Minify.Application/Services/Geolocation/
git commit -m "feat: add IGeolocationService interface"
```

---

## Task 5: Application — analytics use cases

**Files:**
- Create: `src/Minify.Application/UseCases/Analytics/GetSummary/IGetAnalyticsSummaryUseCase.cs`
- Create: `src/Minify.Application/UseCases/Analytics/GetSummary/GetAnalyticsSummaryValidator.cs`
- Create: `src/Minify.Application/UseCases/Analytics/GetSummary/GetAnalyticsSummaryUseCase.cs`
- Create: `src/Minify.Application/UseCases/Analytics/GetAccesses/IGetAnalyticsAccessesUseCase.cs`
- Create: `src/Minify.Application/UseCases/Analytics/GetAccesses/GetAnalyticsAccessesValidator.cs`
- Create: `src/Minify.Application/UseCases/Analytics/GetAccesses/GetAnalyticsAccessesUseCase.cs`

- [ ] **Step 1: Create `IGetAnalyticsSummaryUseCase.cs`**

Create `src/Minify.Application/UseCases/Analytics/GetSummary/IGetAnalyticsSummaryUseCase.cs`:

```csharp
using Minify.Communication.Responses;
using Minify.Communication.Responses.Analytics;

namespace Minify.Application.UseCases.Analytics.GetSummary;

public interface IGetAnalyticsSummaryUseCase
{
    Task<ResultJson<ResponseAnalyticsSummaryJson>> Execute(
        string shortCode,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Create `GetAnalyticsSummaryValidator.cs`**

Create `src/Minify.Application/UseCases/Analytics/GetSummary/GetAnalyticsSummaryValidator.cs`:

```csharp
using FluentValidation;

namespace Minify.Application.UseCases.Analytics.GetSummary;

public record AnalyticsSummaryInput(string ShortCode, DateTime From, DateTime To);

public class GetAnalyticsSummaryValidator : AbstractValidator<AnalyticsSummaryInput>
{
    public GetAnalyticsSummaryValidator()
    {
        RuleFor(x => x.ShortCode)
            .NotEmpty().WithMessage("Short code is required.");

        RuleFor(x => x.To)
            .GreaterThan(x => x.From).WithMessage("'to' must be after 'from'.");
    }
}
```

- [ ] **Step 3: Create `GetAnalyticsSummaryUseCase.cs`**

Create `src/Minify.Application/UseCases/Analytics/GetSummary/GetAnalyticsSummaryUseCase.cs`:

```csharp
using Minify.Communication.Responses;
using Minify.Communication.Responses.Analytics;
using Minify.Domain.Repositories.UrlAccessEvent;

namespace Minify.Application.UseCases.Analytics.GetSummary;

public class GetAnalyticsSummaryUseCase(IUrlAccessEventReadOnlyRepository repository) : IGetAnalyticsSummaryUseCase
{
    public async Task<ResultJson<ResponseAnalyticsSummaryJson>> Execute(
        string shortCode,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var validationResult = Validate<ResponseAnalyticsSummaryJson>(shortCode, from, to);
        if (!validationResult.IsSuccess)
            return validationResult;

        var events = await repository.GetAll(shortCode, from, to, cancellationToken);

        var summary = new ResponseAnalyticsSummaryJson
        {
            TotalClicks = events.Count,
            ByCountry = events
                .Where(e => e.Country is not null)
                .GroupBy(e => e.Country!)
                .Select(g => new CountryClicksJson { Country = g.Key, Clicks = g.Count() })
                .OrderByDescending(x => x.Clicks)
                .ToList(),
            ByDevice = events
                .GroupBy(e => e.DeviceType)
                .Select(g => new DeviceClicksJson { DeviceType = g.Key, Clicks = g.Count() })
                .OrderByDescending(x => x.Clicks)
                .ToList(),
            ByBrowser = events
                .GroupBy(e => e.Browser)
                .Select(g => new BrowserClicksJson { Browser = g.Key, Clicks = g.Count() })
                .OrderByDescending(x => x.Clicks)
                .ToList(),
            BySource = events
                .GroupBy(e => e.TrafficSource)
                .Select(g => new SourceClicksJson { Source = g.Key, Clicks = g.Count() })
                .OrderByDescending(x => x.Clicks)
                .ToList(),
            ByDay = events
                .GroupBy(e => e.AccessedAt.Date)
                .Select(g => new DayClicksJson { Date = g.Key.ToString("yyyy-MM-dd"), Clicks = g.Count() })
                .OrderBy(x => x.Date)
                .ToList()
        };

        return ResultJson<ResponseAnalyticsSummaryJson>.Success(summary);
    }

    private static ResultJson<T> Validate<T>(string shortCode, DateTime from, DateTime to)
    {
        var input = new AnalyticsSummaryInput(shortCode, from, to);
        var result = new GetAnalyticsSummaryValidator().Validate(input);

        if (result.IsValid)
            return ResultJson<T>.Success(default!);

        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        return ResultJson<T>.Failure(errors);
    }
}
```

- [ ] **Step 4: Create `IGetAnalyticsAccessesUseCase.cs`**

Create `src/Minify.Application/UseCases/Analytics/GetAccesses/IGetAnalyticsAccessesUseCase.cs`:

```csharp
using Minify.Communication.Responses;
using Minify.Communication.Responses.Analytics;

namespace Minify.Application.UseCases.Analytics.GetAccesses;

public interface IGetAnalyticsAccessesUseCase
{
    Task<ResultJson<ResponseAnalyticsAccessesJson>> Execute(
        string shortCode,
        DateTime from,
        DateTime to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 5: Create `GetAnalyticsAccessesValidator.cs`**

Create `src/Minify.Application/UseCases/Analytics/GetAccesses/GetAnalyticsAccessesValidator.cs`:

```csharp
using FluentValidation;

namespace Minify.Application.UseCases.Analytics.GetAccesses;

public record AnalyticsAccessesInput(string ShortCode, DateTime From, DateTime To, int Page, int PageSize);

public class GetAnalyticsAccessesValidator : AbstractValidator<AnalyticsAccessesInput>
{
    public GetAnalyticsAccessesValidator()
    {
        RuleFor(x => x.ShortCode)
            .NotEmpty().WithMessage("Short code is required.");

        RuleFor(x => x.To)
            .GreaterThan(x => x.From).WithMessage("'to' must be after 'from'.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("'page' must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200).WithMessage("'pageSize' must be between 1 and 200.");
    }
}
```

- [ ] **Step 6: Create `GetAnalyticsAccessesUseCase.cs`**

Create `src/Minify.Application/UseCases/Analytics/GetAccesses/GetAnalyticsAccessesUseCase.cs`:

```csharp
using Minify.Communication.Responses;
using Minify.Communication.Responses.Analytics;
using Minify.Domain.Repositories.UrlAccessEvent;

namespace Minify.Application.UseCases.Analytics.GetAccesses;

public class GetAnalyticsAccessesUseCase(IUrlAccessEventReadOnlyRepository repository) : IGetAnalyticsAccessesUseCase
{
    public async Task<ResultJson<ResponseAnalyticsAccessesJson>> Execute(
        string shortCode,
        DateTime from,
        DateTime to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var validationResult = Validate<ResponseAnalyticsAccessesJson>(shortCode, from, to, page, pageSize);
        if (!validationResult.IsSuccess)
            return validationResult;

        var (items, total) = await repository.GetPaged(shortCode, from, to, page, pageSize, cancellationToken);

        return ResultJson<ResponseAnalyticsAccessesJson>.Success(new ResponseAnalyticsAccessesJson
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(e => new AccessEventItemJson
            {
                AccessedAt = e.AccessedAt,
                Country = e.Country,
                City = e.City,
                DeviceType = e.DeviceType,
                Browser = e.Browser,
                OperatingSystem = e.OperatingSystem,
                TrafficSource = e.TrafficSource,
                UtmSource = e.UtmSource,
                UtmMedium = e.UtmMedium,
                UtmCampaign = e.UtmCampaign
            }).ToList()
        });
    }

    private static ResultJson<T> Validate<T>(string shortCode, DateTime from, DateTime to, int page, int pageSize)
    {
        var input = new AnalyticsAccessesInput(shortCode, from, to, page, pageSize);
        var result = new GetAnalyticsAccessesValidator().Validate(input);

        if (result.IsValid)
            return ResultJson<T>.Success(default!);

        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        return ResultJson<T>.Failure(errors);
    }
}
```

- [ ] **Step 7: Build and verify**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 8: Commit**

```bash
git add src/Minify.Application/
git commit -m "feat: add analytics use cases (summary and accesses)"
```

---

## Task 6: Infrastructure — EF config, repositories, DbContext

**Files:**
- Create: `src/Minify.Infrastructure/DataAccess/Configurations/UrlAccessEventEntityConfiguration.cs`
- Create: `src/Minify.Infrastructure/DataAccess/Repositories/UrlAccessEvent/UrlAccessEventWriteOnlyRepository.cs`
- Create: `src/Minify.Infrastructure/DataAccess/Repositories/UrlAccessEvent/UrlAccessEventReadOnlyRepository.cs`
- Modify: `src/Minify.Infrastructure/DataAccess/MinifyDbContext.cs`

- [ ] **Step 1: Create `UrlAccessEventEntityConfiguration.cs`**

Create `src/Minify.Infrastructure/DataAccess/Configurations/UrlAccessEventEntityConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Minify.Domain.Entities;

namespace Minify.Infrastructure.DataAccess.Configurations;

public sealed class UrlAccessEventEntityConfiguration : IEntityTypeConfiguration<UrlAccessEventEntity>
{
    public void Configure(EntityTypeBuilder<UrlAccessEventEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ShortCode).IsRequired();
        builder.Property(x => x.AccessedAt).IsRequired();
        builder.Property(x => x.DeviceType).IsRequired();
        builder.Property(x => x.Browser).IsRequired();
        builder.Property(x => x.OperatingSystem).IsRequired();
        builder.Property(x => x.TrafficSource).IsRequired();

        builder.HasOne<ShortUrlEntity>()
            .WithMany()
            .HasForeignKey(x => x.ShortCode)
            .HasPrincipalKey(x => x.ShortenCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ShortCode, x.AccessedAt });
        builder.HasIndex(x => new { x.ShortCode, x.Country });
        builder.HasIndex(x => new { x.ShortCode, x.TrafficSource });
    }
}
```

- [ ] **Step 2: Update `MinifyDbContext.cs`**

Add the `UrlAccessEvents` DbSet and apply its configuration:

```csharp
using Microsoft.EntityFrameworkCore;
using Minify.Domain.Entities;
using Minify.Infrastructure.DataAccess.Configurations;

namespace Minify.Infrastructure.DataAccess;

public class MinifyDbContext(DbContextOptions<MinifyDbContext> options) : DbContext(options)
{
    public DbSet<ShortUrlEntity> ShortUrls => Set<ShortUrlEntity>();
    public DbSet<UrlAccessEventEntity> UrlAccessEvents => Set<UrlAccessEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ShortUrlEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UrlAccessEventEntityConfiguration());
    }
}
```

- [ ] **Step 3: Create `UrlAccessEventWriteOnlyRepository.cs`**

Create `src/Minify.Infrastructure/DataAccess/Repositories/UrlAccessEvent/UrlAccessEventWriteOnlyRepository.cs`:

```csharp
using Minify.Domain.Entities;
using Minify.Domain.Repositories.UrlAccessEvent;
using Minify.Infrastructure.DataAccess;

namespace Minify.Infrastructure.DataAccess.Repositories.UrlAccessEvent;

public class UrlAccessEventWriteOnlyRepository(MinifyDbContext dbContext) : IUrlAccessEventWriteOnlyRepository
{
    public async Task Add(UrlAccessEventEntity entity, CancellationToken cancellationToken = default)
    {
        await dbContext.UrlAccessEvents.AddAsync(entity, cancellationToken);
    }
}
```

Note: this follows the existing pattern — the repository stages the entity; the caller commits via `IUnitOfWork`.

- [ ] **Step 4: Create `UrlAccessEventReadOnlyRepository.cs`**

Create `src/Minify.Infrastructure/DataAccess/Repositories/UrlAccessEvent/UrlAccessEventReadOnlyRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Minify.Domain.Entities;
using Minify.Domain.Repositories.UrlAccessEvent;
using Minify.Infrastructure.DataAccess;

namespace Minify.Infrastructure.DataAccess.Repositories.UrlAccessEvent;

public class UrlAccessEventReadOnlyRepository(MinifyDbContext dbContext) : IUrlAccessEventReadOnlyRepository
{
    public async Task<List<UrlAccessEventEntity>> GetAll(
        string shortCode,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UrlAccessEvents.AsNoTracking()
            .Where(e => e.ShortCode == shortCode && e.AccessedAt >= from && e.AccessedAt <= to)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<UrlAccessEventEntity> Items, int Total)> GetPaged(
        string shortCode,
        DateTime from,
        DateTime to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.UrlAccessEvents.AsNoTracking()
            .Where(e => e.ShortCode == shortCode && e.AccessedAt >= from && e.AccessedAt <= to);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.AccessedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
```

- [ ] **Step 5: Build and verify**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/Minify.Infrastructure/DataAccess/
git commit -m "feat: add UrlAccessEvent EF config, repositories, and DbContext update"
```

---

## Task 7: EF Core migration

**Files:**
- Auto-generated: `src/Minify.Infrastructure/Migrations/` (new migration file)

- [ ] **Step 1: Generate the migration**

```bash
dotnet ef migrations add Analytics-UrlAccessEvents \
  --project src/Minify.Infrastructure \
  --startup-project src/Minify.API
```

Expected output includes: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 2: Inspect the generated migration**

Open the generated file in `src/Minify.Infrastructure/Migrations/`. Verify it contains:
- `CreateTable` for `UrlAccessEvents` with all expected columns (`Id`, `ShortCode`, `AccessedAt`, `IpAddress`, `Country`, `City`, `DeviceType`, `Browser`, `OperatingSystem`, `TrafficSource`, `UtmSource`, `UtmMedium`, `UtmCampaign`, `Referer`)
- `AddForeignKey` referencing `ShortUrls.ShortenCode` with `onDelete: ReferentialAction.Restrict`
- Three `CreateIndex` calls for `(ShortCode, AccessedAt)`, `(ShortCode, Country)`, `(ShortCode, TrafficSource)`

If anything is missing, check `UrlAccessEventEntityConfiguration` from Task 6 and correct it before proceeding.

- [ ] **Step 3: Commit**

```bash
git add src/Minify.Infrastructure/Migrations/
git commit -m "feat: add EF migration for UrlAccessEvents table"
```

---

## Task 8: Infrastructure — `GeolocationService` + `TrafficSourceDetector`

**Files:**
- Create: `src/Minify.Infrastructure/Services/Geolocation/GeolocationService.cs`
- Create: `src/Minify.Infrastructure/Services/Analytics/TrafficSourceDetector.cs`

- [ ] **Step 1: Create `GeolocationService.cs`**

Create `src/Minify.Infrastructure/Services/Geolocation/GeolocationService.cs`:

```csharp
using System.Net.Http.Json;
using Minify.Application.Services.Geolocation;

namespace Minify.Infrastructure.Services.Geolocation;

public class GeolocationService(HttpClient httpClient) : IGeolocationService
{
    public async Task<GeolocationResult?> GetAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<IpApiResponse>(
                $"json/{ipAddress}?fields=status,country,city",
                cancellationToken);

            if (response is null || response.Status != "success")
                return null;

            return new GeolocationResult(response.Country, response.City);
        }
        catch
        {
            return null;
        }
    }

    private record IpApiResponse(string Status, string Country, string City);
}
```

- [ ] **Step 2: Create `TrafficSourceDetector.cs`**

Create `src/Minify.Infrastructure/Services/Analytics/TrafficSourceDetector.cs`:

```csharp
namespace Minify.Infrastructure.Services.Analytics;

public static class TrafficSourceDetector
{
    private static readonly Dictionary<string, string> KnownDomains =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["facebook.com"] = "facebook",
            ["l.facebook.com"] = "facebook",
            ["m.facebook.com"] = "facebook",
            ["wa.me"] = "whatsapp",
            ["web.whatsapp.com"] = "whatsapp",
            ["t.co"] = "twitter",
            ["twitter.com"] = "twitter",
            ["x.com"] = "twitter",
            ["instagram.com"] = "instagram",
            ["l.instagram.com"] = "instagram",
            ["mail.google.com"] = "email",
            ["outlook.live.com"] = "email",
            ["outlook.com"] = "email",
            ["yahoo.com"] = "email"
        };

    public static string Detect(string? utmSource, string? referer)
    {
        if (!string.IsNullOrWhiteSpace(utmSource))
            return utmSource.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(referer))
            return "direct";

        if (!Uri.TryCreate(referer, UriKind.Absolute, out var uri))
            return "direct";

        return KnownDomains.TryGetValue(uri.Host, out var source) ? source : "organic";
    }
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/Minify.Infrastructure/Services/Geolocation/ src/Minify.Infrastructure/Services/Analytics/
git commit -m "feat: add GeolocationService and TrafficSourceDetector"
```

---

## Task 9: Infrastructure — `RabbitMqEventPublisher`

**Files:**
- Create: `src/Minify.Infrastructure/Services/Messaging/RabbitMqEventPublisher.cs`
- Modify: `src/Minify.Infrastructure/Minify.Infrastructure.csproj`

- [ ] **Step 1: Install MassTransit packages**

```bash
dotnet add src/Minify.Infrastructure/Minify.Infrastructure.csproj package MassTransit
dotnet add src/Minify.Infrastructure/Minify.Infrastructure.csproj package MassTransit.RabbitMQ
```

- [ ] **Step 2: Create `RabbitMqEventPublisher.cs`**

Create `src/Minify.Infrastructure/Services/Messaging/RabbitMqEventPublisher.cs`:

```csharp
using MassTransit;
using Minify.Messaging.Publishers;

namespace Minify.Infrastructure.Services.Messaging;

public class RabbitMqEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        await publishEndpoint.Publish(message, cancellationToken);
    }
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/Minify.Infrastructure/Services/Messaging/RabbitMqEventPublisher.cs \
        src/Minify.Infrastructure/Minify.Infrastructure.csproj
git commit -m "feat: add RabbitMqEventPublisher with MassTransit"
```

---

## Task 10: Infrastructure — `UrlClickedEventConsumer`

**Files:**
- Create: `src/Minify.Infrastructure/Services/Messaging/UrlClickedEventConsumer.cs`

- [ ] **Step 1: Install UAParser**

```bash
dotnet add src/Minify.Infrastructure/Minify.Infrastructure.csproj package UAParser
```

- [ ] **Step 2: Create `UrlClickedEventConsumer.cs`**

Create `src/Minify.Infrastructure/Services/Messaging/UrlClickedEventConsumer.cs`:

```csharp
using MassTransit;
using Minify.Application.Services.Geolocation;
using Minify.Domain.Entities;
using Minify.Domain.Repositories;
using Minify.Domain.Repositories.UrlAccessEvent;
using Minify.Infrastructure.Services.Analytics;
using Minify.Messaging.Events;
using UAParser;

namespace Minify.Infrastructure.Services.Messaging;

public class UrlClickedEventConsumer(
    IGeolocationService geolocationService,
    IUrlAccessEventWriteOnlyRepository repository,
    IUnitOfWork unitOfWork) : IConsumer<UrlClickedEvent>
{
    private static readonly Parser UaParser = Parser.GetDefault();

    public async Task Consume(ConsumeContext<UrlClickedEvent> context)
    {
        var ev = context.Message;

        var clientInfo = UaParser.Parse(ev.UserAgent ?? string.Empty);
        var deviceType = ResolveDeviceType(clientInfo);
        var trafficSource = TrafficSourceDetector.Detect(ev.UtmSource, ev.Referer);

        GeolocationResult? geo = null;
        if (!string.IsNullOrWhiteSpace(ev.IpAddress))
            geo = await geolocationService.GetAsync(ev.IpAddress, context.CancellationToken);

        var entity = new UrlAccessEventEntity
        {
            Id = Guid.NewGuid(),
            ShortCode = ev.ShortCode,
            AccessedAt = ev.Timestamp,
            IpAddress = ev.IpAddress,
            Country = geo?.Country,
            City = geo?.City,
            DeviceType = deviceType,
            Browser = clientInfo.UA.Family,
            OperatingSystem = clientInfo.OS.Family,
            TrafficSource = trafficSource,
            UtmSource = ev.UtmSource,
            UtmMedium = ev.UtmMedium,
            UtmCampaign = ev.UtmCampaign,
            Referer = ev.Referer
        };

        await repository.Add(entity, context.CancellationToken);
        await unitOfWork.Commit(context.CancellationToken);
    }

    private static string ResolveDeviceType(ClientInfo clientInfo)
    {
        if (clientInfo.Device.IsSpider)
            return "Bot";
        if (clientInfo.Device.Family.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            return "Tablet";
        if (clientInfo.OS.Family is "iOS" or "Android")
            return "Mobile";
        if (clientInfo.Device.Family != "Other")
            return "Mobile";
        return "Desktop";
    }
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/Minify.Infrastructure/Services/Messaging/UrlClickedEventConsumer.cs \
        src/Minify.Infrastructure/Minify.Infrastructure.csproj
git commit -m "feat: add UrlClickedEventConsumer with UAParser"
```

---

## Task 11: DI registration

**Files:**
- Modify: `src/Minify.Infrastructure/DependencyInjection.cs`
- Modify: `src/Minify.Application/DependencyInjection.cs`

- [ ] **Step 1: Update `Minify.Infrastructure/DependencyInjection.cs`**

Replace the entire file with:

```csharp
using HashidsNet;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minify.Application.Services.Caching;
using Minify.Application.Services.Geolocation;
using Minify.Application.Services.ShortCode;
using Minify.Domain.Repositories;
using Minify.Domain.Repositories.ShortUrl;
using Minify.Domain.Repositories.UrlAccessEvent;
using Minify.Infrastructure.DataAccess;
using Minify.Infrastructure.DataAccess.Repositories.ShortUrl;
using Minify.Infrastructure.DataAccess.Repositories.UrlAccessEvent;
using Minify.Infrastructure.Services.Caching;
using Minify.Infrastructure.Services.Geolocation;
using Minify.Infrastructure.Services.Messaging;
using Minify.Infrastructure.Services.ShortCode;
using Minify.Messaging.Publishers;
using StackExchange.Redis;

namespace Minify.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddRedis(services, configuration);
        AddRepositories(services);
        AddHashids(services, configuration);
        AddShortCodeServices(services);
        AddGeolocationService(services, configuration);
        AddMessaging(services, configuration);

        return services;
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        services.AddDbContext<MinifyDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHostedService<DatabaseInitializer>();
    }

    private static void AddRedis(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis") ??
                                    throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IShortUrlWriteOnlyRepository, ShortUrlWriteOnlyRepository>();
        services.AddScoped<IShortUrlReadOnlyRepository, ShortUrlReadOnlyRepository>();
        services.AddScoped<IUrlAccessEventWriteOnlyRepository, UrlAccessEventWriteOnlyRepository>();
        services.AddScoped<IUrlAccessEventReadOnlyRepository, UrlAccessEventReadOnlyRepository>();
    }

    private static void AddHashids(IServiceCollection services, IConfiguration configuration)
    {
        var salt = configuration.GetValue<string>("Hashids:Salt");
        if (string.IsNullOrWhiteSpace(salt))
            throw new InvalidOperationException("'Salt' for Hashids is not configured.");

        var minLength = configuration.GetValue<int>("Hashids:MinLength");
        if (minLength <= 0) throw new InvalidOperationException("'MinLength' for Hashids is not configured.");

        services.AddSingleton<IHashids>(_ => new Hashids(salt, minLength));
    }

    private static void AddShortCodeServices(IServiceCollection services)
    {
        services.AddSingleton<IShortCodeGenerator, ShortCodeGenerator>();
        services.AddSingleton<IUrlCacheService, UrlCacheService>();

        services.AddHostedService<ShortCodeCounterInitializer>();
    }

    private static void AddGeolocationService(IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Geolocation:ApiUrl"] ?? "http://ip-api.com/";
        var timeoutMs = configuration.GetValue<int>("Geolocation:TimeoutMs", 500);

        services.AddHttpClient<IGeolocationService, GeolocationService>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
        });
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ") ??
                                       throw new InvalidOperationException("Connection string 'RabbitMQ' is not configured.");

        services.AddMassTransit(x =>
        {
            x.AddConsumer<UrlClickedEventConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqConnectionString);

                cfg.ReceiveEndpoint("url-clicked", e =>
                {
                    e.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(15),
                        TimeSpan.FromSeconds(45)));

                    e.ConfigureConsumer<UrlClickedEventConsumer>(context);
                });
            });
        });

        services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
    }
}
```

- [ ] **Step 2: Update `Minify.Application/DependencyInjection.cs`**

Replace the entire file with:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Minify.Application.UseCases.Analytics.GetAccesses;
using Minify.Application.UseCases.Analytics.GetSummary;
using Minify.Application.UseCases.Url.RetrieveOriginalUrl;
using Minify.Application.UseCases.Url.ShortenUrl;

namespace Minify.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        AddUseCases(services);

        return services;
    }

    private static void AddUseCases(IServiceCollection services)
    {
        services.AddScoped<IShortenUrlUseCase, ShortenUrlUseCase>();
        services.AddScoped<IRetrieveOriginalUrlUseCase, RetrieveOriginalUrlUseCase>();
        services.AddScoped<IGetAnalyticsSummaryUseCase, GetAnalyticsSummaryUseCase>();
        services.AddScoped<IGetAnalyticsAccessesUseCase, GetAnalyticsAccessesUseCase>();
    }
}
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/Minify.Infrastructure/DependencyInjection.cs src/Minify.Application/DependencyInjection.cs
git commit -m "feat: register analytics repositories, geolocation service, and MassTransit"
```

---

## Task 12: API — `AnalyticsEndpoints` + `Program.cs`

**Files:**
- Create: `src/Minify.API/Endpoints/AnalyticsEndpoints.cs`
- Modify: `src/Minify.API/Program.cs`

- [ ] **Step 1: Create `AnalyticsEndpoints.cs`**

Create `src/Minify.API/Endpoints/AnalyticsEndpoints.cs`:

```csharp
using Minify.Application.UseCases.Analytics.GetAccesses;
using Minify.Application.UseCases.Analytics.GetSummary;
using Minify.Communication.Responses;

namespace Minify.API.Endpoints;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/analytics/{shortCode}/summary",
            async (string shortCode,
                   DateTime from,
                   DateTime to,
                   IGetAnalyticsSummaryUseCase useCase,
                   CancellationToken cancellationToken) =>
            {
                var result = await useCase.Execute(shortCode, from, to, cancellationToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(ResultJson.Failure(result.Errors));

                return Results.Ok(result.Data);
            });

        app.MapGet("/api/analytics/{shortCode}/accesses",
            async (string shortCode,
                   DateTime from,
                   DateTime to,
                   IGetAnalyticsAccessesUseCase useCase,
                   int page = 1,
                   int pageSize = 50,
                   CancellationToken cancellationToken = default) =>
            {
                var result = await useCase.Execute(shortCode, from, to, page, pageSize, cancellationToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(ResultJson.Failure(result.Errors));

                return Results.Ok(result.Data);
            });

        return app;
    }
}
```

- [ ] **Step 2: Update `Program.cs`**

Add `app.MapAnalyticsEndpoints()` after `app.MapUrlEndpoints()`:

```csharp
using Minify.API.Endpoints;
using Minify.Application;
using Minify.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapUrlEndpoints();
app.MapAnalyticsEndpoints();

app.Run();
```

- [ ] **Step 3: Build and verify**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/Minify.API/Endpoints/AnalyticsEndpoints.cs src/Minify.API/Program.cs
git commit -m "feat: add analytics endpoints and register them in Program.cs"
```

---

## Task 13: API — update redirect endpoint to publish event

**Files:**
- Modify: `src/Minify.API/Endpoints/UrlEndpoints.cs`

- [ ] **Step 1: Update `UrlEndpoints.cs`**

Replace the entire file with:

```csharp
using Minify.Application.UseCases.Url.RetrieveOriginalUrl;
using Minify.Application.UseCases.Url.ShortenUrl;
using Minify.Communication.Requests.Url;
using Minify.Communication.Responses;
using Minify.Communication.Responses.Url;
using Minify.Messaging.Events;
using Minify.Messaging.Publishers;

namespace Minify.API.Endpoints;

public static class UrlEndpoints
{
    public static IEndpointRouteBuilder MapUrlEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{shortenedCode}",
            async (string shortenedCode,
                   HttpContext httpContext,
                   IRetrieveOriginalUrlUseCase useCase,
                   IEventPublisher eventPublisher,
                   ILogger<Program> logger,
                   CancellationToken cancellationToken) =>
            {
                var result = await useCase.Execute(shortenedCode, cancellationToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(ResultJson.Failure(result.Errors));

                if (result.Data.Status == RetrieveOriginalUrlStatus.Found)
                {
                    var query = httpContext.Request.Query;
                    var ev = new UrlClickedEvent
                    {
                        ShortCode = shortenedCode,
                        Timestamp = DateTime.UtcNow,
                        IpAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                                    ?? httpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                        Referer = httpContext.Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : null,
                        UtmSource = query["utm_source"].ToString() is { Length: > 0 } s ? s : null,
                        UtmMedium = query["utm_medium"].ToString() is { Length: > 0 } m ? m : null,
                        UtmCampaign = query["utm_campaign"].ToString() is { Length: > 0 } c ? c : null
                    };

                    try
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
                        await eventPublisher.PublishAsync(ev, cts.Token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to publish UrlClickedEvent for {ShortCode}", shortenedCode);
                    }
                }

                return result.Data.Status switch
                {
                    RetrieveOriginalUrlStatus.Found => Results.Redirect(result.Data.LongUrl, permanent: false),
                    RetrieveOriginalUrlStatus.Expired => Results.StatusCode(StatusCodes.Status410Gone),
                    _ => Results.NotFound()
                };
            });

        app.MapPost("/api/shorten-url",
            async (RequestShortenUrlJson request, IShortenUrlUseCase useCase, CancellationToken cancellationToken) =>
            {
                var result = await useCase.Execute(request, cancellationToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(ResultJson.Failure(result.Errors));

                return Results.Created($"/{result.Data.ShortenCode}", result);
            });

        return app;
    }
}
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Minify.API/Endpoints/UrlEndpoints.cs
git commit -m "feat: publish UrlClickedEvent on successful redirect"
```

---

## Task 14: Docker Compose + appsettings

**Files:**
- Modify: `docker-compose.yml`
- Modify: `src/Minify.API/appsettings.json`
- Modify: `src/Minify.API/appsettings.Development.json`

- [ ] **Step 1: Update `docker-compose.yml`**

Add the RabbitMQ service and wire it into the `api` dependencies:

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__Postgres: >-
        Host=db;Port=5432;Database=minify;Username=minify;Password=minify
      ConnectionStrings__Redis: redis:6379
      ConnectionStrings__RabbitMQ: amqp://minify:minify@rabbitmq:5672
      Hashids__Salt: G5rS0r#O=4PD')|+gp<fMbsr22#C%qp_?FnuGPgp4uYBOSnM
      Hashids__MinLength: 4
      Geolocation__ApiUrl: http://ip-api.com/
      Geolocation__TimeoutMs: 500
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    restart: unless-stopped

  db:
    image: postgres:17-alpine
    ports:
      - "5432:5432"
    environment:
      POSTGRES_DB: minify
      POSTGRES_USER: minify
      POSTGRES_PASSWORD: minify
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U minify -d minify"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 5s
    restart: unless-stopped

  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: minify
      RABBITMQ_DEFAULT_PASS: minify
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 20s
    restart: unless-stopped

volumes:
  postgres_data:
  redis_data:
  rabbitmq_data:
```

- [ ] **Step 2: Update `appsettings.json`**

Add the new configuration keys (values are placeholders — real values go in `appsettings.Development.json` and environment variables):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Geolocation": {
    "ApiUrl": "http://ip-api.com/",
    "TimeoutMs": 500
  }
}
```

- [ ] **Step 3: Update `appsettings.Development.json`**

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=minify;Username=minify;Password=minify",
    "Redis": "localhost:6379",
    "RabbitMQ": "amqp://minify:minify@localhost:5672"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Hashids": {
    "Salt": "G5rS0r#O=4PD')|+gp<fMbsr22#C%qp_?FnuGPgp4uYBOSnM",
    "MinLength": 4
  },
  "Geolocation": {
    "ApiUrl": "http://ip-api.com/",
    "TimeoutMs": 500
  }
}
```

- [ ] **Step 4: Verify full stack starts**

```bash
docker compose up --build
```

Expected: all four services (`db`, `redis`, `rabbitmq`, `api`) reach healthy state. Check `http://localhost:15672` (RabbitMQ management UI, login `minify`/`minify`) — the `url-clicked` queue should appear after the first redirect.

- [ ] **Step 5: Smoke test**

```bash
# 1. Shorten a URL
curl -s -X POST http://localhost:8080/api/shorten-url \
  -H "Content-Type: application/json" \
  -d '{"url":"https://example.com","expiresAt":"2027-01-01T00:00:00Z"}' | jq .

# 2. Redirect (replace <code> with the code returned above)
curl -v http://localhost:8080/<code>?utm_source=facebook

# 3. Query analytics (wait ~2 seconds for consumer to process)
curl -s "http://localhost:8080/api/analytics/<code>/summary?from=2026-01-01T00:00:00Z&to=2027-01-01T00:00:00Z" | jq .
```

Expected from step 3: `totalClicks: 1`, `bySource` contains `facebook`.

- [ ] **Step 6: Commit**

```bash
git add docker-compose.yml src/Minify.API/appsettings.json src/Minify.API/appsettings.Development.json
git commit -m "feat: add RabbitMQ to docker-compose and update appsettings for analytics"
```
