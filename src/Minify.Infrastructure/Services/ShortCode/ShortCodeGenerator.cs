using HashidsNet;
using Minify.Application.Services.ShortCode;
using Minify.Infrastructure.Services.Caching;
using StackExchange.Redis;

namespace Minify.Infrastructure.Services.ShortCode;

public class ShortCodeGenerator(IConnectionMultiplexer redis, IHashids hashids) : IShortCodeGenerator
{
    public async Task<string> Generate(CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        var nextId = await database.StringIncrementAsync(RedisKeys.Counter);

        return hashids.EncodeLong(nextId);
    }
}
