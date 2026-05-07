using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Minify.Infrastructure.DataAccess;
using StackExchange.Redis;

namespace WebApi.Test.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public required string PostgresConnectionString { get; init; }
    public required string RedisConnectionString { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var descriptor in hostedServices)
                services.Remove(descriptor);

            var dbContextOptions = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<MinifyDbContext>));
            if (dbContextOptions is not null)
                services.Remove(dbContextOptions);

            services.AddDbContext<MinifyDbContext>(options =>
                options.UseNpgsql(PostgresConnectionString));

            var redisDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor is not null)
                services.Remove(redisDescriptor);

            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(RedisConnectionString));
        });
    }
}
