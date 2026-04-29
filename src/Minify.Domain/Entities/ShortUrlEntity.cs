namespace Minify.Domain.Entities;

public class ShortUrlEntity
{
    public string ShortenCode { get; set; } = string.Empty;
    public string LongUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
