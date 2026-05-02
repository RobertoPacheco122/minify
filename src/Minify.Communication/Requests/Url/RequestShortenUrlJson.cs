namespace Minify.Communication.Requests.Url;

public class RequestShortenUrlJson
{
    public string Url { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(1);
}
