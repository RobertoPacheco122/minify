using Minify.Domain.Entities;
using Minify.Domain.Repositories.UrlAccessEvent;

namespace Minify.Infrastructure.DataAccess.Repositories.UrlAccessEvent;

public class UrlAccessEventWriteOnlyRepository(MinifyDbContext dbContext) : IUrlAccessEventWriteOnlyRepository
{
    public async Task Add(UrlAccessEventEntity entity, CancellationToken cancellationToken = default)
    {
        await dbContext.UrlAccessEvents.AddAsync(entity, cancellationToken);
    }
}
