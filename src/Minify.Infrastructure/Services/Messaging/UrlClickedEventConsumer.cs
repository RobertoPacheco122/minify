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
