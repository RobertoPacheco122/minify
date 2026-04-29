using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Minify.Domain.Entities;

namespace Minify.Infrastructure.DataAccess.Configurations;

public sealed class ShortUrlEntityConfiguration : IEntityTypeConfiguration<ShortUrlEntity>
{
    public void Configure(EntityTypeBuilder<ShortUrlEntity> builder)
    {
        builder.HasKey(x => x.ShortenCode);

        builder.Property(x => x.ShortenCode)
            .IsRequired();

        builder.Property(x => x.LongUrl)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.Property(x => x.ExpiresAt)
            .IsRequired();
    }
}
