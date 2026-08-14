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
