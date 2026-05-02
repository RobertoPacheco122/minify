namespace Minify.Infrastructure.Services.Caching;

public class RedisKeys
{
    public const string Counter = "minify:short-url:counter";
    
    public static string GenerateShortCodeUrlCacheKey(string shortCode) => $"minify:short-url:{shortCode}";
}
