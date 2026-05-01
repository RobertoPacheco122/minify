using Minify.Domain.Entities;

namespace Minify.Domain.Repositories.ShortUrl;

public interface IShortUrlReadOnlyRepository
{
    Task<ShortUrlEntity?> GetByShortCode(string shortCode, CancellationToken cancellationToken = default);
}
