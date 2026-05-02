using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Minify.Infrastructure.DataAccess;

public class MinifyDbContextFactory : IDesignTimeDbContextFactory<MinifyDbContext>
{
    public MinifyDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Postgres");
        
        var options = new DbContextOptionsBuilder<MinifyDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        
        return new MinifyDbContext(options);
    }
}
