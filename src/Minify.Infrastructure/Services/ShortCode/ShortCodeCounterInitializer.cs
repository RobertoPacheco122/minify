using HashidsNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Minify.Infrastructure.Services.Caching;
using StackExchange.Redis;

namespace Minify.Infrastructure.Services.ShortCode;

public class ShortCodeCounterInitializer(
    IConnectionMultiplexer redis,
    IConfiguration configuration,
    ILogger<ShortCodeCounterInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var salt = configuration.GetValue<string>("Hashids:Salt");
        if (string.IsNullOrEmpty(salt)) throw new InvalidOperationException("'Salt' for Hashids is not configured.");

        var startingValue = CalculateStartingValue(salt);

        var database = redis.GetDatabase();

        var initializedCountNow = await database.StringSetAsync(RedisKeys.Counter, startingValue, when: When.NotExists);
        if (!initializedCountNow) return;

        logger.LogInformation("ShortCode counter initialized with value: {Value}", startingValue);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static long CalculateStartingValue(string salt)
    {
        var probe = new Hashids(salt);

        long candidate = 1;
        while (probe.EncodeLong(candidate).Length < 4)
            candidate++;

        return candidate;
    }
}
