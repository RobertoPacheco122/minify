using Minify.Domain.Entities;

namespace Minify.Domain.Repositories.UrlAccessEvent;

public interface IUrlAccessEventReadOnlyRepository
{
    Task<List<UrlAccessEventEntity>> GetAll(
        string shortCode,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<(List<UrlAccessEventEntity> Items, int Total)> GetPaged(
        string shortCode,
        DateTime from,
        DateTime to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
