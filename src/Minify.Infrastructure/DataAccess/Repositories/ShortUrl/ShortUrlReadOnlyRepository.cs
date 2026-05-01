using Minify.Domain.Entities;
using Minify.Domain.Repositories.ShortUrl;

namespace Minify.Infrastructure.DataAccess.Repositories.ShortUrl;

public class ShortUrlReadOnlyRepository(MinifyDbContext dbContext) : IShortUrlReadOnlyRepository
{
    public async Task<ShortUrlEntity?> GetByShortCode(string shortCode)
    {
        return await dbContext.ShortUrls.FindAsync(shortCode);
    }
}
