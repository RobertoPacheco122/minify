using Minify.Domain.Repositories;
using Moq;

namespace CommonTestUtilities.Mocks;

public class UnitOfWorkMock
{
    public Mock<IUnitOfWork> Mock { get; } = new();

    public UnitOfWorkMock()
    {
        Mock.Setup(u => u.Commit(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
