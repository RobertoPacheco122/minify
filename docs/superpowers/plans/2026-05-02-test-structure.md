# Test Structure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create 4 test projects (CommonTestUtilities, UseCases.Test, Validators.Test, WebApi.Test) covering validators, use cases, and full API end-to-end with real PostgreSQL and Redis via Testcontainers.

**Architecture:** Unit tests (validators, use cases) instantiate classes directly with Moq mocks from CommonTestUtilities. WebApi.Test uses `WebApplicationFactory<Program>` + Testcontainers to boot a real app and run HTTP assertions against it.

**Tech Stack:** xUnit, Moq, FluentAssertions, Testcontainers.PostgreSql, Testcontainers.Redis, Microsoft.AspNetCore.Mvc.Testing, HashidsNet, StackExchange.Redis

---

## File Map

| File | Action |
|------|--------|
| `src/Minify.API/Program.cs` | Modify — append `public partial class Program {}` |
| `tests/Minify.CommonTestUtilities/Minify.CommonTestUtilities.csproj` | Create |
| `tests/Minify.CommonTestUtilities/Builders/Entities/ShortUrlEntityBuilder.cs` | Create |
| `tests/Minify.CommonTestUtilities/Builders/Requests/RequestShortenUrlJsonBuilder.cs` | Create |
| `tests/Minify.CommonTestUtilities/Mocks/Repositories/ShortUrlReadOnlyRepositoryMock.cs` | Create |
| `tests/Minify.CommonTestUtilities/Mocks/Repositories/ShortUrlWriteOnlyRepositoryMock.cs` | Create |
| `tests/Minify.CommonTestUtilities/Mocks/Services/UrlCacheServiceMock.cs` | Create |
| `tests/Minify.CommonTestUtilities/Mocks/Services/ShortCodeGeneratorMock.cs` | Create |
| `tests/Minify.CommonTestUtilities/Mocks/UnitOfWorkMock.cs` | Create |
| `tests/Minify.UseCases.Test/Minify.UseCases.Test.csproj` | Create |
| `tests/Minify.UseCases.Test/UseCases/Url/ShortenUrl/ShortenUrlUseCaseTests.cs` | Create |
| `tests/Minify.UseCases.Test/UseCases/Url/RetrieveOriginalUrl/RetrieveOriginalUrlUseCaseTests.cs` | Create |
| `tests/Minify.Validators.Test/Minify.Validators.Test.csproj` | Create |
| `tests/Minify.Validators.Test/Validators/Url/ShortenUrlValidatorTests.cs` | Create |
| `tests/Minify.Validators.Test/Validators/Url/RetrieveOriginalUrlValidatorTests.cs` | Create |
| `tests/Minify.WebApi.Test/Minify.WebApi.Test.csproj` | Create |
| `tests/Minify.WebApi.Test/Infrastructure/CustomWebApplicationFactory.cs` | Create |
| `tests/Minify.WebApi.Test/Infrastructure/MinifyApiFixture.cs` | Create |
| `tests/Minify.WebApi.Test/Controllers/Url/ShortenUrlTests.cs` | Create |
| `tests/Minify.WebApi.Test/Controllers/Url/RetrieveOriginalUrlTests.cs` | Create |

---

## Task 1: Scaffold all 4 test projects

**Files:**
- Create: `tests/Minify.CommonTestUtilities/Minify.CommonTestUtilities.csproj`
- Create: `tests/Minify.UseCases.Test/Minify.UseCases.Test.csproj`
- Create: `tests/Minify.Validators.Test/Minify.Validators.Test.csproj`
- Create: `tests/Minify.WebApi.Test/Minify.WebApi.Test.csproj`

- [ ] **Step 1: Create projects**

Run from the repo root (`/Users/robertopacheco/RiderProjects/Minify`):

```bash
dotnet new classlib -n Minify.CommonTestUtilities -o tests/Minify.CommonTestUtilities --framework net10.0
dotnet new xunit -n Minify.UseCases.Test -o tests/Minify.UseCases.Test --framework net10.0
dotnet new xunit -n Minify.Validators.Test -o tests/Minify.Validators.Test --framework net10.0
dotnet new xunit -n Minify.WebApi.Test -o tests/Minify.WebApi.Test --framework net10.0
```

- [ ] **Step 2: Delete template placeholder files**

```bash
rm tests/Minify.CommonTestUtilities/Class1.cs
rm tests/Minify.UseCases.Test/UnitTest1.cs
rm tests/Minify.Validators.Test/UnitTest1.cs
rm tests/Minify.WebApi.Test/UnitTest1.cs
```

- [ ] **Step 3: Add all projects to the solution**

```bash
dotnet sln add tests/Minify.CommonTestUtilities/Minify.CommonTestUtilities.csproj
dotnet sln add tests/Minify.UseCases.Test/Minify.UseCases.Test.csproj
dotnet sln add tests/Minify.Validators.Test/Minify.Validators.Test.csproj
dotnet sln add tests/Minify.WebApi.Test/Minify.WebApi.Test.csproj
```

- [ ] **Step 4: Add packages to CommonTestUtilities**

```bash
dotnet add tests/Minify.CommonTestUtilities package Moq
dotnet add tests/Minify.CommonTestUtilities reference src/Minify.Application/Minify.Application.csproj
dotnet add tests/Minify.CommonTestUtilities reference src/Minify.Domain/Minify.Domain.csproj
dotnet add tests/Minify.CommonTestUtilities reference src/Minify.Communication/Minify.Communication.csproj
```

- [ ] **Step 5: Add packages to UseCases.Test**

```bash
dotnet add tests/Minify.UseCases.Test package FluentAssertions
dotnet add tests/Minify.UseCases.Test reference tests/Minify.CommonTestUtilities/Minify.CommonTestUtilities.csproj
dotnet add tests/Minify.UseCases.Test reference src/Minify.Application/Minify.Application.csproj
```

- [ ] **Step 6: Add packages to Validators.Test**

```bash
dotnet add tests/Minify.Validators.Test package FluentAssertions
dotnet add tests/Minify.Validators.Test reference tests/Minify.CommonTestUtilities/Minify.CommonTestUtilities.csproj
dotnet add tests/Minify.Validators.Test reference src/Minify.Application/Minify.Application.csproj
```

- [ ] **Step 7: Add packages to WebApi.Test**

```bash
dotnet add tests/Minify.WebApi.Test package FluentAssertions
dotnet add tests/Minify.WebApi.Test package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/Minify.WebApi.Test package Testcontainers.PostgreSql
dotnet add tests/Minify.WebApi.Test package Testcontainers.Redis
dotnet add tests/Minify.WebApi.Test package HashidsNet
dotnet add tests/Minify.WebApi.Test package StackExchange.Redis
dotnet add tests/Minify.WebApi.Test reference tests/Minify.CommonTestUtilities/Minify.CommonTestUtilities.csproj
dotnet add tests/Minify.WebApi.Test reference src/Minify.API/Minify.API.csproj
dotnet add tests/Minify.WebApi.Test reference src/Minify.Infrastructure/Minify.Infrastructure.csproj
```

- [ ] **Step 8: Verify solution builds**

```bash
dotnet build
```

Expected: Build succeeded with 0 errors (warnings about empty projects are OK).

---

## Task 2: CommonTestUtilities — Builders

**Files:**
- Create: `tests/Minify.CommonTestUtilities/Builders/Entities/ShortUrlEntityBuilder.cs`
- Create: `tests/Minify.CommonTestUtilities/Builders/Requests/RequestShortenUrlJsonBuilder.cs`

- [ ] **Step 1: Create ShortUrlEntityBuilder**

```csharp
// tests/Minify.CommonTestUtilities/Builders/Entities/ShortUrlEntityBuilder.cs
using Minify.Domain.Entities;

namespace Minify.CommonTestUtilities.Builders.Entities;

public static class ShortUrlEntityBuilder
{
    public static ShortUrlEntity Build(string? longUrl = null, DateTime? expiresAt = null) =>
        new()
        {
            ShortenCode = "abcd",
            LongUrl = longUrl ?? "https://example.com",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(1)
        };

    public static ShortUrlEntity BuildExpired() =>
        new()
        {
            ShortenCode = "abcd",
            LongUrl = "https://example.com",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
}
```

- [ ] **Step 2: Create RequestShortenUrlJsonBuilder**

```csharp
// tests/Minify.CommonTestUtilities/Builders/Requests/RequestShortenUrlJsonBuilder.cs
using Minify.Communication.Requests.Url;

namespace Minify.CommonTestUtilities.Builders.Requests;

public static class RequestShortenUrlJsonBuilder
{
    public static RequestShortenUrlJson Build() =>
        new()
        {
            Url = "https://example.com",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

    public static RequestShortenUrlJson BuildWithInvalidUrl() =>
        new()
        {
            Url = "not-a-url",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

    public static RequestShortenUrlJson BuildWithPastExpiration() =>
        new()
        {
            Url = "https://example.com",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
}
```

- [ ] **Step 3: Verify build**

```bash
dotnet build tests/Minify.CommonTestUtilities
```

Expected: Build succeeded with 0 errors.

---

## Task 3: CommonTestUtilities — Mocks

**Files:**
- Create: `tests/Minify.CommonTestUtilities/Mocks/Repositories/ShortUrlReadOnlyRepositoryMock.cs`
- Create: `tests/Minify.CommonTestUtilities/Mocks/Repositories/ShortUrlWriteOnlyRepositoryMock.cs`
- Create: `tests/Minify.CommonTestUtilities/Mocks/Services/UrlCacheServiceMock.cs`
- Create: `tests/Minify.CommonTestUtilities/Mocks/Services/ShortCodeGeneratorMock.cs`
- Create: `tests/Minify.CommonTestUtilities/Mocks/UnitOfWorkMock.cs`

- [ ] **Step 1: Create ShortUrlReadOnlyRepositoryMock**

```csharp
// tests/Minify.CommonTestUtilities/Mocks/Repositories/ShortUrlReadOnlyRepositoryMock.cs
using Minify.Domain.Entities;
using Minify.Domain.Repositories.ShortUrl;
using Moq;

namespace Minify.CommonTestUtilities.Mocks.Repositories;

public class ShortUrlReadOnlyRepositoryMock
{
    public Mock<IShortUrlReadOnlyRepository> Mock { get; } = new();

    public void SetupFound(ShortUrlEntity entity) =>
        Mock.Setup(r => r.GetByShortCode(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

    public void SetupNotFound() =>
        Mock.Setup(r => r.GetByShortCode(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShortUrlEntity?)null);
}
```

- [ ] **Step 2: Create ShortUrlWriteOnlyRepositoryMock**

```csharp
// tests/Minify.CommonTestUtilities/Mocks/Repositories/ShortUrlWriteOnlyRepositoryMock.cs
using Minify.Domain.Entities;
using Minify.Domain.Repositories.ShortUrl;
using Moq;

namespace Minify.CommonTestUtilities.Mocks.Repositories;

public class ShortUrlWriteOnlyRepositoryMock
{
    public Mock<IShortUrlWriteOnlyRepository> Mock { get; } = new();

    public ShortUrlWriteOnlyRepositoryMock()
    {
        Mock.Setup(r => r.Add(It.IsAny<ShortUrlEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
```

- [ ] **Step 3: Create UrlCacheServiceMock**

```csharp
// tests/Minify.CommonTestUtilities/Mocks/Services/UrlCacheServiceMock.cs
using Minify.Application.Services.Caching;
using Moq;

namespace Minify.CommonTestUtilities.Mocks.Services;

public class UrlCacheServiceMock
{
    public Mock<IUrlCacheService> Mock { get; } = new();

    public UrlCacheServiceMock()
    {
        Mock.Setup(c => c.Set(It.IsAny<string>(), It.IsAny<CachedUrl>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public void SetupFound(CachedUrl cachedUrl) =>
        Mock.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedUrl);

    public void SetupNotFound() =>
        Mock.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CachedUrl?)null);
}
```

- [ ] **Step 4: Create ShortCodeGeneratorMock**

```csharp
// tests/Minify.CommonTestUtilities/Mocks/Services/ShortCodeGeneratorMock.cs
using Minify.Application.Services.ShortCode;
using Moq;

namespace Minify.CommonTestUtilities.Mocks.Services;

public class ShortCodeGeneratorMock
{
    public Mock<IShortCodeGenerator> Mock { get; } = new();

    public ShortCodeGeneratorMock(string code = "abcd")
    {
        Mock.Setup(g => g.Generate(It.IsAny<CancellationToken>()))
            .ReturnsAsync(code);
    }
}
```

- [ ] **Step 5: Create UnitOfWorkMock**

```csharp
// tests/Minify.CommonTestUtilities/Mocks/UnitOfWorkMock.cs
using Minify.Domain.Repositories;
using Moq;

namespace Minify.CommonTestUtilities.Mocks;

public class UnitOfWorkMock
{
    public Mock<IUnitOfWork> Mock { get; } = new();

    public UnitOfWorkMock()
    {
        Mock.Setup(u => u.Commit(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
```

- [ ] **Step 6: Verify build**

```bash
dotnet build tests/Minify.CommonTestUtilities
```

Expected: Build succeeded with 0 errors.

---

## Task 4: Validators.Test

**Files:**
- Create: `tests/Minify.Validators.Test/Validators/Url/ShortenUrlValidatorTests.cs`
- Create: `tests/Minify.Validators.Test/Validators/Url/RetrieveOriginalUrlValidatorTests.cs`

- [ ] **Step 1: Create ShortenUrlValidatorTests**

```csharp
// tests/Minify.Validators.Test/Validators/Url/ShortenUrlValidatorTests.cs
using FluentAssertions;
using Minify.Application.UseCases.Url.ShortenUrl;
using Minify.CommonTestUtilities.Builders.Requests;

namespace Minify.Validators.Test.Validators.Url;

public class ShortenUrlValidatorTests
{
    private readonly ShortenUrlValidator _validator = new();

    [Fact]
    public void Should_Pass_When_RequestIsValid()
    {
        var request = RequestShortenUrlJsonBuilder.Build();

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_UrlIsEmpty()
    {
        var request = RequestShortenUrlJsonBuilder.Build();
        request.Url = string.Empty;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }

    [Fact]
    public void Should_Fail_When_UrlIsRelative()
    {
        var request = RequestShortenUrlJsonBuilder.Build();
        request.Url = "/path/only";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }

    [Fact]
    public void Should_Fail_When_UrlSchemeIsNotHttpOrHttps()
    {
        var request = RequestShortenUrlJsonBuilder.Build();
        request.Url = "ftp://example.com";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }

    [Fact]
    public void Should_Fail_When_ExpiresAtIsInThePast()
    {
        var request = RequestShortenUrlJsonBuilder.BuildWithPastExpiration();

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpiresAt");
    }

    [Fact]
    public void Should_Fail_When_ExpiresAtIsNow()
    {
        var request = RequestShortenUrlJsonBuilder.Build();
        request.ExpiresAt = DateTime.UtcNow;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpiresAt");
    }
}
```

- [ ] **Step 2: Create RetrieveOriginalUrlValidatorTests**

```csharp
// tests/Minify.Validators.Test/Validators/Url/RetrieveOriginalUrlValidatorTests.cs
using FluentAssertions;
using Minify.Application.UseCases.Url.RetrieveOriginalUrl;

namespace Minify.Validators.Test.Validators.Url;

public class RetrieveOriginalUrlValidatorTests
{
    private readonly RetrieveOriginalUrlValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CodeIsValid()
    {
        var result = _validator.Validate("abcd");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_CodeIsEmpty()
    {
        var result = _validator.Validate(string.Empty);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeIsTooShort()
    {
        var result = _validator.Validate("abc");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeIsTooLong()
    {
        var result = _validator.Validate("abcdefgh");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeHasSpecialCharacters()
    {
        var result = _validator.Validate("ab!cd");

        result.IsValid.Should().BeFalse();
    }
}
```

- [ ] **Step 3: Run Validators.Test**

```bash
dotnet test tests/Minify.Validators.Test
```

Expected: 11 tests passed, 0 failed.

---

## Task 5: UseCases.Test

**Files:**
- Create: `tests/Minify.UseCases.Test/UseCases/Url/ShortenUrl/ShortenUrlUseCaseTests.cs`
- Create: `tests/Minify.UseCases.Test/UseCases/Url/RetrieveOriginalUrl/RetrieveOriginalUrlUseCaseTests.cs`

- [ ] **Step 1: Create ShortenUrlUseCaseTests**

```csharp
// tests/Minify.UseCases.Test/UseCases/Url/ShortenUrl/ShortenUrlUseCaseTests.cs
using FluentAssertions;
using Minify.Application.UseCases.Url.ShortenUrl;
using Minify.CommonTestUtilities.Builders.Requests;
using Minify.CommonTestUtilities.Mocks;
using Minify.CommonTestUtilities.Mocks.Repositories;
using Minify.CommonTestUtilities.Mocks.Services;
using Moq;

namespace Minify.UseCases.Test.UseCases.Url.ShortenUrl;

public class ShortenUrlUseCaseTests
{
    private readonly ShortUrlWriteOnlyRepositoryMock _writeRepo = new();
    private readonly ShortCodeGeneratorMock _codeGen = new();
    private readonly UnitOfWorkMock _unitOfWork = new();

    private ShortenUrlUseCase CreateUseCase() =>
        new(_writeRepo.Mock.Object, _codeGen.Mock.Object, _unitOfWork.Mock.Object);

    [Fact]
    public async Task Should_ReturnSuccess_When_RequestIsValid()
    {
        var request = RequestShortenUrlJsonBuilder.Build();
        var useCase = CreateUseCase();

        var result = await useCase.Execute(request);

        result.IsSuccess.Should().BeTrue();
        result.Data.ShortenCode.Should().Be("abcd");
        result.Data.ExpiresAt.Should().Be(request.ExpiresAt);
        _unitOfWork.Mock.Verify(u => u.Commit(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnError_When_UrlIsInvalid()
    {
        var request = RequestShortenUrlJsonBuilder.BuildWithInvalidUrl();
        var useCase = CreateUseCase();

        var result = await useCase.Execute(request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        _unitOfWork.Mock.Verify(u => u.Commit(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_ReturnError_When_ExpiresAtIsInThePast()
    {
        var request = RequestShortenUrlJsonBuilder.BuildWithPastExpiration();
        var useCase = CreateUseCase();

        var result = await useCase.Execute(request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        _unitOfWork.Mock.Verify(u => u.Commit(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

- [ ] **Step 2: Create RetrieveOriginalUrlUseCaseTests**

```csharp
// tests/Minify.UseCases.Test/UseCases/Url/RetrieveOriginalUrl/RetrieveOriginalUrlUseCaseTests.cs
using FluentAssertions;
using Minify.Application.Services.Caching;
using Minify.Application.UseCases.Url.RetrieveOriginalUrl;
using Minify.CommonTestUtilities.Builders.Entities;
using Minify.CommonTestUtilities.Mocks.Repositories;
using Minify.CommonTestUtilities.Mocks.Services;
using Minify.Communication.Responses.Url;
using Moq;

namespace Minify.UseCases.Test.UseCases.Url.RetrieveOriginalUrl;

public class RetrieveOriginalUrlUseCaseTests
{
    private readonly ShortUrlReadOnlyRepositoryMock _readRepo = new();
    private readonly UrlCacheServiceMock _cacheService = new();

    private RetrieveOriginalUrlUseCase CreateUseCase() =>
        new(_readRepo.Mock.Object, _cacheService.Mock.Object);

    [Fact]
    public async Task Should_ReturnFound_When_UrlExistsInCache()
    {
        var cachedUrl = new CachedUrl { LongUrl = "https://example.com", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _cacheService.SetupFound(cachedUrl);
        var useCase = CreateUseCase();

        var result = await useCase.Execute("abcd");

        result.IsSuccess.Should().BeTrue();
        result.Data.Status.Should().Be(RetrieveOriginalUrlStatus.Found);
        result.Data.LongUrl.Should().Be("https://example.com");
        _readRepo.Mock.Verify(
            r => r.GetByShortCode(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_ReturnFound_When_UrlExistsInDatabase()
    {
        var entity = ShortUrlEntityBuilder.Build();
        _cacheService.SetupNotFound();
        _readRepo.SetupFound(entity);
        var useCase = CreateUseCase();

        var result = await useCase.Execute("abcd");

        result.IsSuccess.Should().BeTrue();
        result.Data.Status.Should().Be(RetrieveOriginalUrlStatus.Found);
        _cacheService.Mock.Verify(
            c => c.Set(It.IsAny<string>(), It.IsAny<CachedUrl>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UrlDoesNotExist()
    {
        _cacheService.SetupNotFound();
        _readRepo.SetupNotFound();
        var useCase = CreateUseCase();

        var result = await useCase.Execute("abcd");

        result.IsSuccess.Should().BeTrue();
        result.Data.Status.Should().Be(RetrieveOriginalUrlStatus.NotFound);
    }

    [Fact]
    public async Task Should_ReturnExpired_When_UrlIsExpired()
    {
        var entity = ShortUrlEntityBuilder.BuildExpired();
        _cacheService.SetupNotFound();
        _readRepo.SetupFound(entity);
        var useCase = CreateUseCase();

        var result = await useCase.Execute("abcd");

        result.IsSuccess.Should().BeTrue();
        result.Data.Status.Should().Be(RetrieveOriginalUrlStatus.Expired);
    }
}
```

- [ ] **Step 3: Run UseCases.Test**

```bash
dotnet test tests/Minify.UseCases.Test
```

Expected: 7 tests passed, 0 failed.

---

## Task 6: WebApi.Test — Infrastructure

**Files:**
- Modify: `src/Minify.API/Program.cs`
- Create: `tests/Minify.WebApi.Test/Infrastructure/CustomWebApplicationFactory.cs`
- Create: `tests/Minify.WebApi.Test/Infrastructure/MinifyApiFixture.cs`

- [ ] **Step 1: Expose Program class for WebApplicationFactory**

Append this line to the end of `src/Minify.API/Program.cs`:

```csharp
public partial class Program { }
```

The full file should now look like:

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

app.Run();

public partial class Program { }
```

- [ ] **Step 2: Create CustomWebApplicationFactory**

```csharp
// tests/Minify.WebApi.Test/Infrastructure/CustomWebApplicationFactory.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Minify.WebApi.Test.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public required string PostgresConnectionString { get; init; }
    public required string RedisConnectionString { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = PostgresConnectionString,
                ["ConnectionStrings:Redis"] = RedisConnectionString,
                ["Hashids:Salt"] = "test-salt",
                ["Hashids:MinLength"] = "4"
            });
        });

        builder.ConfigureServices(services =>
        {
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var descriptor in hostedServices)
                services.Remove(descriptor);
        });
    }
}
```

- [ ] **Step 3: Create MinifyApiFixture**

```csharp
// tests/Minify.WebApi.Test/Infrastructure/MinifyApiFixture.cs
using HashidsNet;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Minify.Infrastructure.DataAccess;
using Minify.Infrastructure.Services.Caching;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Minify.WebApi.Test.Infrastructure;

public class MinifyApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithDatabase("minify_test")
        .WithUsername("minify")
        .WithPassword("minify")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder().Build();

    private CustomWebApplicationFactory _factory = null!;

    public HttpClient HttpClient { get; private set; } = null!;
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync()
        );

        _factory = new CustomWebApplicationFactory
        {
            PostgresConnectionString = _postgresContainer.GetConnectionString(),
            RedisConnectionString = _redisContainer.GetConnectionString()
        };

        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<MinifyDbContext>();
        await dbContext.Database.MigrateAsync();

        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();

        var probe = new Hashids("test-salt");
        long candidate = 1;
        while (probe.EncodeLong(candidate).Length < 4)
            candidate++;

        await db.StringSetAsync(RedisKeys.Counter, candidate, when: When.NotExists);

        HttpClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task DisposeAsync()
    {
        HttpClient.Dispose();
        await _factory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }
}
```

- [ ] **Step 4: Verify WebApi.Test builds**

```bash
dotnet build tests/Minify.WebApi.Test
```

Expected: Build succeeded with 0 errors.

---

## Task 7: WebApi.Test — ShortenUrl endpoint tests

**Files:**
- Create: `tests/Minify.WebApi.Test/Controllers/Url/ShortenUrlTests.cs`

- [ ] **Step 1: Create ShortenUrlTests**

```csharp
// tests/Minify.WebApi.Test/Controllers/Url/ShortenUrlTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Minify.CommonTestUtilities.Builders.Requests;
using Minify.Infrastructure.DataAccess;
using Minify.WebApi.Test.Infrastructure;

namespace Minify.WebApi.Test.Controllers.Url;

public class ShortenUrlTests(MinifyApiFixture fixture) : IClassFixture<MinifyApiFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Should_Return201_When_RequestIsValid()
    {
        var request = RequestShortenUrlJsonBuilder.Build();

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/shorten-url", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ShortenUrlResponse>(JsonOptions);
        body!.IsSuccess.Should().BeTrue();
        body.Data!.ShortenCode.Should().NotBeNullOrEmpty();
        body.Data.ExpiresAt.Should().BeCloseTo(request.ExpiresAt, TimeSpan.FromSeconds(1));

        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MinifyDbContext>();
        var exists = await dbContext.ShortUrls.AnyAsync(u => u.ShortenCode == body.Data.ShortenCode);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Return400_When_UrlIsInvalid()
    {
        var request = RequestShortenUrlJsonBuilder.BuildWithInvalidUrl();

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/shorten-url", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_Return400_When_ExpiresAtIsInThePast()
    {
        var request = RequestShortenUrlJsonBuilder.BuildWithPastExpiration();

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/shorten-url", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private record ShortenUrlResponse(bool IsSuccess, List<string> Errors, ShortenUrlData? Data);
    private record ShortenUrlData(string ShortenCode, DateTime ExpiresAt);
}
```

- [ ] **Step 2: Run ShortenUrlTests only**

```bash
dotnet test tests/Minify.WebApi.Test --filter "FullyQualifiedName~ShortenUrlTests"
```

Expected: 3 tests passed, 0 failed. Containers start and stop automatically.

---

## Task 8: WebApi.Test — RetrieveOriginalUrl endpoint tests

**Files:**
- Create: `tests/Minify.WebApi.Test/Controllers/Url/RetrieveOriginalUrlTests.cs`

- [ ] **Step 1: Create RetrieveOriginalUrlTests**

```csharp
// tests/Minify.WebApi.Test/Controllers/Url/RetrieveOriginalUrlTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Minify.CommonTestUtilities.Builders.Entities;
using Minify.CommonTestUtilities.Builders.Requests;
using Minify.Infrastructure.DataAccess;
using Minify.WebApi.Test.Infrastructure;

namespace Minify.WebApi.Test.Controllers.Url;

public class RetrieveOriginalUrlTests(MinifyApiFixture fixture) : IClassFixture<MinifyApiFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Should_Return302_When_CodeExists()
    {
        var createRequest = RequestShortenUrlJsonBuilder.Build();
        var createResponse = await fixture.HttpClient.PostAsJsonAsync("/api/shorten-url", createRequest);
        var body = await createResponse.Content.ReadFromJsonAsync<ShortenUrlResponse>(JsonOptions);
        var shortenCode = body!.Data!.ShortenCode;

        var response = await fixture.HttpClient.GetAsync($"/{shortenCode}");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.OriginalString.Should().Be(createRequest.Url);
    }

    [Fact]
    public async Task Should_Return404_When_CodeDoesNotExist()
    {
        var response = await fixture.HttpClient.GetAsync("/zzzz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_Return410_When_UrlIsExpired()
    {
        var expiredEntity = ShortUrlEntityBuilder.BuildExpired();
        expiredEntity.ShortenCode = "wxyz";

        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MinifyDbContext>();
        dbContext.ShortUrls.Add(expiredEntity);
        await dbContext.SaveChangesAsync();

        var response = await fixture.HttpClient.GetAsync($"/{expiredEntity.ShortenCode}");

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }

    private record ShortenUrlResponse(bool IsSuccess, List<string> Errors, ShortenUrlData? Data);
    private record ShortenUrlData(string ShortenCode, DateTime ExpiresAt);
}
```

- [ ] **Step 2: Run all WebApi.Test**

```bash
dotnet test tests/Minify.WebApi.Test
```

Expected: 6 tests passed, 0 failed.

- [ ] **Step 3: Run all tests in the solution**

```bash
dotnet test
```

Expected: 24 tests passed, 0 failed (11 validators + 7 use cases + 6 webapi).

---

## Self-Review Notes

- All types match exactly: `ShortenUrlEntity`, `RequestShortenUrlJson`, `CachedUrl`, `ResultJson<T>`, `RetrieveOriginalUrlStatus`
- `ShortenUrlUseCase` constructor: `(IShortUrlWriteOnlyRepository, IShortCodeGenerator, IUnitOfWork)` — matches Task 5 step 1
- `RetrieveOriginalUrlUseCase` constructor: `(IShortUrlReadOnlyRepository, IUrlCacheService)` — matches Task 5 step 2
- `RedisKeys.Counter` constant used in `MinifyApiFixture` — defined in `Minify.Infrastructure.Services.Caching`
- `WebApi.Test` explicitly references both `Minify.API` and `Minify.Infrastructure` (types from both are used directly)
- `AllowAutoRedirect = false` is set in `MinifyApiFixture` so 302 assertions work correctly
- `Should_Return410_When_UrlIsExpired` inserts `"wxyz"` directly — a 4-char alphanumeric code that passes the validator but is not generated by the counter, so it won't collide with `Should_Return302_When_CodeExists`
