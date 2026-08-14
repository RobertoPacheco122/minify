using CommonTestUtilities.Mocks.Publishers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Minify.Application.UseCases.Analytics.TrackClick;
using Minify.Messaging.Events;
using Moq;

namespace UseCases.Test.UseCases.Analytics.TrackClick;

public class TrackUrlClickUseCaseTests
{
    private readonly EventPublisherMock _eventPublisher = new();
    private readonly Mock<ILogger<TrackUrlClickUseCase>> _logger = new();

    private TrackUrlClickUseCase CreateUseCase() =>
        new(_eventPublisher.Mock.Object, _logger.Object);

    [Fact]
    public async Task Should_PublishEvent_When_InputHasAllFields()
    {
        var input = new TrackUrlClickInput(
            "abcd", "127.0.0.1", "curl/8.0", "https://facebook.com", "facebook", "cpc", "summer");
        var useCase = CreateUseCase();

        await useCase.Execute(input);

        _eventPublisher.Mock.Verify(
            p => p.PublishAsync(
                It.Is<UrlClickedEvent>(e =>
                    e.ShortCode == "abcd" &&
                    e.IpAddress == "127.0.0.1" &&
                    e.UserAgent == "curl/8.0" &&
                    e.Referer == "https://facebook.com" &&
                    e.UtmSource == "facebook" &&
                    e.UtmMedium == "cpc" &&
                    e.UtmCampaign == "summer"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_NormalizeEmptyStrings_To_Null()
    {
        var input = new TrackUrlClickInput("abcd", null, "curl/8.0", "", "", "", "");
        var useCase = CreateUseCase();

        await useCase.Execute(input);

        _eventPublisher.Mock.Verify(
            p => p.PublishAsync(
                It.Is<UrlClickedEvent>(e =>
                    e.Referer == null &&
                    e.UtmSource == null &&
                    e.UtmMedium == null &&
                    e.UtmCampaign == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_NotThrow_When_PublisherFails()
    {
        _eventPublisher.SetupThrows(new InvalidOperationException("broker unavailable"));
        var input = new TrackUrlClickInput("abcd", null, null, null, null, null, null);
        var useCase = CreateUseCase();

        var act = async () => await useCase.Execute(input);

        await act.Should().NotThrowAsync();
    }
}
