using Minify.Domain.Entities;

namespace Minify.Domain.Repositories.ShortUrl;

public interface IShortUrlWriteOnlyRepository
{
    Task Add(ShortUrlEntity entity);
}
