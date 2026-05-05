using CommonTestUtilities.Builders.Requests.Url;
using CommonTestUtilities.Mocks;
using CommonTestUtilities.Mocks.Repositories.ShortUrl;
using CommonTestUtilities.Mocks.Services.ShortCode;
using FluentAssertions;
using Minify.Application.UseCases.Url.ShortenUrl;
using Moq;

namespace UseCases.Test.UseCases.Url.ShortenUrl;

public class ShortenUrlUseCaseTests
{
    private readonly ShortUrlWriteOnlyRepositoryMock _writeOnlyRepository = new();
    private readonly ShortCodeGeneratorMock _codeGeneratorMock = new();
    private readonly UnitOfWorkMock _unitOfWork = new();

    private ShortenUrlUseCase CreateUseCase() => new(_writeOnlyRepository.Mock.Object, _codeGeneratorMock.Mock.Object,
        _unitOfWork.Mock.Object);
    
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
    public async Task Should_ReturnError_When_UrlIsEmpty()
    {
        var request = RequestShortenUrlJsonBuilder.BuildWithInvalidUrl();
        request.Url = string.Empty;
        var useCase = CreateUseCase();

        var result = await useCase.Execute(request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain("Property 'url' must be a valid absolute http or https URL.");
        _unitOfWork.Mock.Verify(u => u.Commit(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("/path/only")]
    [InlineData("ftp://example.com")]
    public async Task Should_ReturnError_When_UrlIsInvalid(string url)
    {
        var request = RequestShortenUrlJsonBuilder.BuildWithInvalidUrl();
        request.Url = url;
        var useCase = CreateUseCase();

        var result = await useCase.Execute(request);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain("Property 'url' must be a valid absolute http or https URL.");
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
        result.Errors.Should().Contain("Property 'expiresAt' must be a date in the future.");
        _unitOfWork.Mock.Verify(u => u.Commit(It.IsAny<CancellationToken>()), Times.Never);
    }
}
