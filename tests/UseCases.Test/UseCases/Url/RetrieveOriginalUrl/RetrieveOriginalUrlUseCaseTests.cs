using CommonTestUtilities.Builders.Entities;
using CommonTestUtilities.Mocks.Repositories.ShortUrl;
using CommonTestUtilities.Mocks.Services.Caching;
using FluentAssertions;
using Minify.Application.Services.Caching;
using Minify.Application.UseCases.Url.RetrieveOriginalUrl;
using Minify.Communication.Responses.Url;
using Moq;

namespace UseCases.Test.UseCases.Url.RetrieveOriginalUrl;

public class RetrieveOriginalUrlUseCaseTests
{
    private readonly ShortUrlReadOnlyRepositoryMock _readOnlyRepository = new();
    private readonly UrlCacheServiceMock _cacheService = new();

    private RetrieveOriginalUrlUseCase CreateUseCase() =>
        new(_readOnlyRepository.Mock.Object, _cacheService.Mock.Object);

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
        _readOnlyRepository.Mock.Verify(
            r => r.GetByShortCode(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_ReturnFound_When_UrlExistsInDatabase()
    {
        var entity = ShortUrlEntityBuilder.Build();
        _cacheService.SetupNotFound();
        _readOnlyRepository.SetupFound(entity);
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
        _readOnlyRepository.SetupNotFound();
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
        _readOnlyRepository.SetupFound(entity);
        var useCase = CreateUseCase();

        var result = await useCase.Execute("abcd");

        result.IsSuccess.Should().BeTrue();
        result.Data.Status.Should().Be(RetrieveOriginalUrlStatus.Expired);
    }
}
