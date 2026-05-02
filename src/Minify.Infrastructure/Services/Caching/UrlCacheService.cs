using System.Text.Json;
using Minify.Application.Services.Caching;
using StackExchange.Redis;

namespace Minify.Infrastructure.Services.Caching;

public class UrlCacheService(IConnectionMultiplexer redis) : IUrlCacheService
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<CachedUrl?> Get(string shortCode, CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        var value = await database.StringGetAsync(RedisKeys.GenerateShortCodeUrlCacheKey(shortCode));

        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<CachedUrl>(value.ToString(), _serializerOptions);
    }

    public async Task Set(string shortCode, CachedUrl url, CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        var payload = JsonSerializer.Serialize(url, _serializerOptions);

        await database.StringSetAsync(key: RedisKeys.GenerateShortCodeUrlCacheKey(shortCode), value: payload,
            expiry: url.ExpiresAt);
    }
}
