using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minify.Domain.Repositories;
using Minify.Domain.Repositories.ShortUrl;
using Minify.Infrastructure.DataAccess;
using Minify.Infrastructure.DataAccess.Repositories.ShortUrl;

namespace Minify.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddRepositories(services);
        
        return services;
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        services.AddDbContext<MinifyDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHostedService<DatabaseInitializer>();
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IShortUrlWriteOnlyRepository, ShortUrlWriteOnlyRepository>();
    }
}
