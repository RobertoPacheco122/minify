namespace Minify.Application.UseCases.Analytics.TrackClick;

public record TrackUrlClickInput(
    string ShortCode,
    string? IpAddress,
    string? UserAgent,
    string? Referer,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign);
