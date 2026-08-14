using Microsoft.EntityFrameworkCore;
using Minify.Domain.Entities;
using Minify.Domain.Repositories.UrlAccessEvent;

namespace Minify.Infrastructure.DataAccess.Repositories.UrlAccessEvent;

public class UrlAccessEventReadOnlyRepository(MinifyDbContext dbContext) : IUrlAccessEventReadOnlyRepository
{
    public async Task<List<UrlAccessEventEntity>> GetAll(
        string shortCode,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UrlAccessEvents.AsNoTracking()
            .Where(e => e.ShortCode == shortCode && e.AccessedAt >= from && e.AccessedAt <= to)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<UrlAccessEventEntity> Items, int Total)> GetPaged(
        string shortCode,
        DateTime from,
        DateTime to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.UrlAccessEvents.AsNoTracking()
            .Where(e => e.ShortCode == shortCode && e.AccessedAt >= from && e.AccessedAt <= to);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.AccessedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
