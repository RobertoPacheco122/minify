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
