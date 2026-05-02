using Minify.Domain.Entities;
using Minify.Domain.Repositories.ShortUrl;

namespace Minify.Infrastructure.DataAccess.Repositories.ShortUrl;

public class ShortUrlWriteOnlyRepository(MinifyDbContext dbContext) : IShortUrlWriteOnlyRepository
{
    public async Task Add(ShortUrlEntity entity, CancellationToken cancellationToken = default)
    {
        await dbContext.ShortUrls.AddAsync(entity, cancellationToken);
    }
}
