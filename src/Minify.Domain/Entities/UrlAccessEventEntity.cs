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
