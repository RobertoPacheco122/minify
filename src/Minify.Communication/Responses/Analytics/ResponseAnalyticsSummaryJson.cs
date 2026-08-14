namespace Minify.Communication.Responses.Analytics;

public class ResponseAnalyticsSummaryJson
{
    public int TotalClicks { get; set; }
    public List<CountryClicksJson> ByCountry { get; set; } = [];
    public List<DeviceClicksJson> ByDevice { get; set; } = [];
    public List<BrowserClicksJson> ByBrowser { get; set; } = [];
    public List<SourceClicksJson> BySource { get; set; } = [];
    public List<DayClicksJson> ByDay { get; set; } = [];
}

public class CountryClicksJson
{
    public string Country { get; set; } = string.Empty;
    public int Clicks { get; set; }
}

public class DeviceClicksJson
{
    public string DeviceType { get; set; } = string.Empty;
    public int Clicks { get; set; }
}

public class BrowserClicksJson
{
    public string Browser { get; set; } = string.Empty;
    public int Clicks { get; set; }
}

public class SourceClicksJson
{
    public string Source { get; set; } = string.Empty;
    public int Clicks { get; set; }
}

public class DayClicksJson
{
    public string Date { get; set; } = string.Empty;
    public int Clicks { get; set; }
}
