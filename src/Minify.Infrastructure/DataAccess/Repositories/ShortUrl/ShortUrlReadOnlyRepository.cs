using Microsoft.EntityFrameworkCore;
using Minify.Domain.Entities;
using Minify.Domain.Repositories.ShortUrl;

namespace Minify.Infrastructure.DataAccess.Repositories.ShortUrl;

public class ShortUrlReadOnlyRepository(MinifyDbContext dbContext) : IShortUrlReadOnlyRepository
{
    public async Task<ShortUrlEntity?> GetByShortCode(string shortCode, CancellationToken cancellationToken = default)
    {
        return await dbContext.ShortUrls.FirstOrDefaultAsync(url => url.ShortenCode.Equals(shortCode), cancellationToken);
    }
}
