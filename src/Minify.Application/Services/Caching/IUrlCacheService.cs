namespace Minify.Application.Services.Caching;

public interface IUrlCacheService
{
    Task<CachedUrl?> Get(string shortCode, CancellationToken cancellationToken = default);
    Task Set(string shortCode, CachedUrl url, CancellationToken cancellationToken = default);
}
