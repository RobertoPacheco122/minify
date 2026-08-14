using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Minify.Domain.Entities;

namespace Minify.Infrastructure.DataAccess.Configurations;

public sealed class UrlAccessEventEntityConfiguration : IEntityTypeConfiguration<UrlAccessEventEntity>
{
    public void Configure(EntityTypeBuilder<UrlAccessEventEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ShortCode).IsRequired();
        builder.Property(x => x.AccessedAt).IsRequired();
        builder.Property(x => x.DeviceType).IsRequired();
        builder.Property(x => x.Browser).IsRequired();
        builder.Property(x => x.OperatingSystem).IsRequired();
        builder.Property(x => x.TrafficSource).IsRequired();

        builder.HasOne<ShortUrlEntity>()
            .WithMany()
            .HasForeignKey(x => x.ShortCode)
            .HasPrincipalKey(x => x.ShortenCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ShortCode, x.AccessedAt });
        builder.HasIndex(x => new { x.ShortCode, x.Country });
        builder.HasIndex(x => new { x.ShortCode, x.TrafficSource });
    }
}
