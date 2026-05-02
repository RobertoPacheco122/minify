using HashidsNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minify.Application.Services.Caching;
using Minify.Application.Services.ShortCode;
using Minify.Domain.Repositories;
using Minify.Domain.Repositories.ShortUrl;
using Minify.Infrastructure.DataAccess;
using Minify.Infrastructure.DataAccess.Repositories.ShortUrl;
using Minify.Infrastructure.Services.Caching;
using Minify.Infrastructure.Services.ShortCode;
using StackExchange.Redis;

namespace Minify.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddRedis(services, configuration);
        AddRepositories(services);
        AddHashids(services, configuration);
        AddShortCodeServices(services);

        return services;
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        services.AddDbContext<MinifyDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHostedService<DatabaseInitializer>();
    }

    private static void AddRedis(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis") ??
                                    throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IShortUrlWriteOnlyRepository, ShortUrlWriteOnlyRepository>();
        services.AddScoped<IShortUrlReadOnlyRepository, ShortUrlReadOnlyRepository>();
    }

    private static void AddHashids(IServiceCollection services, IConfiguration configuration)
    {
        var salt = configuration.GetValue<string>("Hashids:Salt");
        if (string.IsNullOrWhiteSpace(salt))
            throw new InvalidOperationException("'Salt' for Hashids is not configured.");

        var minLength = configuration.GetValue<int>("Hashids:MinLength");
        if (minLength <= 0) throw new InvalidOperationException("'MinLength' for Hashids is not configured.");

        services.AddSingleton<IHashids>(_ => new Hashids(salt, minLength));
    }

    private static void AddShortCodeServices(IServiceCollection services)
    {
        services.AddSingleton<IShortCodeGenerator, ShortCodeGenerator>();
        services.AddSingleton<IUrlCacheService, UrlCacheService>();

        services.AddHostedService<ShortCodeCounterInitializer>();
    }
}
