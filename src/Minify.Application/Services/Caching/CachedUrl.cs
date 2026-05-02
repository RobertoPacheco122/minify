namespace Minify.Application.Services.Caching;

public class CachedUrl
{
    public string LongUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
