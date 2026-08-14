using Minify.Domain.Entities;

namespace Minify.Domain.Repositories.UrlAccessEvent;

public interface IUrlAccessEventWriteOnlyRepository
{
    Task Add(UrlAccessEventEntity entity, CancellationToken cancellationToken = default);
}
