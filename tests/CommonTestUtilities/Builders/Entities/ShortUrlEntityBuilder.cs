using Minify.Domain.Entities;

namespace CommonTestUtilities.Builders.Entities;

public static class ShortUrlEntityBuilder
{
    public static ShortUrlEntity Build(string? longUrl = null, DateTime? expiresAt = null) => new()
    {
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(1),
        LongUrl = longUrl ?? "https://example.com",
        ShortenCode = "abcd",
    };

    public static ShortUrlEntity BuildExpired() => Build(expiresAt: DateTime.UtcNow.AddDays(-1));
}
