using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Minify.Infrastructure.DataAccess;
using Minify.Infrastructure.Services.Caching;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace WebApi.Test.Infrastructure;

public class MinifyApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithDatabase("minify_test")
        .WithUsername("minify")
        .WithPassword("minify")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder().Build();

    private CustomWebApplicationFactory _factory = null!;

    public HttpClient HttpClient { get; private set; } = null!;
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync()
        );

        _factory = new CustomWebApplicationFactory
        {
            PostgresConnectionString = _postgresContainer.GetConnectionString(),
            RedisConnectionString = _redisContainer.GetConnectionString()
        };

        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<MinifyDbContext>();
        await dbContext.Database.MigrateAsync();

        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();

        var hashids = scope.ServiceProvider.GetRequiredService<HashidsNet.IHashids>();
        long candidate = 1;
        while (hashids.EncodeLong(candidate).Length < 4)
            candidate++;

        await db.StringSetAsync(RedisKeys.Counter, candidate, when: When.NotExists);

        HttpClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public async Task DisposeAsync()
    {
        HttpClient.Dispose();
        await _factory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }
}
