using Microsoft.EntityFrameworkCore;
using Minify.Domain.Entities;
using Minify.Infrastructure.DataAccess.Configurations;

namespace Minify.Infrastructure.DataAccess;

public class MinifyDbContext(DbContextOptions<MinifyDbContext> options) : DbContext(options)
{
    public DbSet<ShortUrlEntity> ShortUrls => Set<ShortUrlEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ShortUrlEntityConfiguration());
    }
}
