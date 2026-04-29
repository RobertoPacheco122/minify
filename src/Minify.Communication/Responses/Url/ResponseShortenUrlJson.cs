namespace Minify.Communication.Responses.Url;

public class ResponseShortenUrlJson
{
    public string ShortenCode { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
