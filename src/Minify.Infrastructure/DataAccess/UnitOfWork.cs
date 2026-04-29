using Minify.Domain.Repositories;

namespace Minify.Infrastructure.DataAccess;

public class UnitOfWork(MinifyDbContext dbContext) : IUnitOfWork
{
    public async Task Commit(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
