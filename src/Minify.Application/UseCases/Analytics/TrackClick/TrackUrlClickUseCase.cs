using Microsoft.Extensions.Logging;
using Minify.Messaging.Events;
using Minify.Messaging.Publishers;

namespace Minify.Application.UseCases.Analytics.TrackClick;

public class TrackUrlClickUseCase(
    IEventPublisher eventPublisher,
    ILogger<TrackUrlClickUseCase> logger) : ITrackUrlClickUseCase
{
    private static readonly TimeSpan PublishTimeout = TimeSpan.FromMilliseconds(100);

    public async Task Execute(TrackUrlClickInput input, CancellationToken cancellationToken = default)
    {
        var ev = new UrlClickedEvent
        {
            ShortCode = input.ShortCode,
            Timestamp = DateTime.UtcNow,
            IpAddress = input.IpAddress,
            UserAgent = input.UserAgent,
            Referer = Normalize(input.Referer),
            UtmSource = Normalize(input.UtmSource),
            UtmMedium = Normalize(input.UtmMedium),
            UtmCampaign = Normalize(input.UtmCampaign)
        };

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(PublishTimeout);
            await eventPublisher.PublishAsync(ev, cts.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish UrlClickedEvent for {ShortCode}", input.ShortCode);
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
