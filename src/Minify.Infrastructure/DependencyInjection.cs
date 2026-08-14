using HashidsNet;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minify.Application.Services.Caching;
using Minify.Application.Services.Geolocation;
using Minify.Application.Services.ShortCode;
using Minify.Domain.Repositories;
using Minify.Domain.Repositories.ShortUrl;
using Minify.Domain.Repositories.UrlAccessEvent;
using Minify.Infrastructure.DataAccess;
using Minify.Infrastructure.DataAccess.Repositories.ShortUrl;
using Minify.Infrastructure.DataAccess.Repositories.UrlAccessEvent;
using Minify.Infrastructure.Services.Caching;
using Minify.Infrastructure.Services.Geolocation;
using Minify.Infrastructure.Services.Messaging;
using Minify.Infrastructure.Services.ShortCode;
using Minify.Messaging.Publishers;
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
        AddGeolocationService(services, configuration);
        AddMessaging(services, configuration);

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
        services.AddScoped<IUrlAccessEventWriteOnlyRepository, UrlAccessEventWriteOnlyRepository>();
        services.AddScoped<IUrlAccessEventReadOnlyRepository, UrlAccessEventReadOnlyRepository>();
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

    private static void AddGeolocationService(IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = configuration["Geolocation:ApiUrl"] ?? "http://ip-api.com/";
        var timeoutMs = configuration.GetValue<int>("Geolocation:TimeoutMs", 500);

        services.AddHttpClient<IGeolocationService, GeolocationService>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
        });
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ") ??
                                       throw new InvalidOperationException("Connection string 'RabbitMQ' is not configured.");

        services.AddMassTransit(x =>
        {
            x.AddConsumer<UrlClickedEventConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqConnectionString);

                cfg.ReceiveEndpoint("url-clicked", e =>
                {
                    e.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(15),
                        TimeSpan.FromSeconds(45)));

                    e.ConfigureConsumer<UrlClickedEventConsumer>(context);
                });
            });
        });

        services.AddScoped<IEventPublisher, RabbitMqEventPublisher>();
    }
}
