using Minify.Domain.Entities;
using Minify.Domain.Repositories.ShortUrl;
using Moq;

namespace CommonTestUtilities.Mocks.Repositories.ShortUrl;

public class ShortUrlWriteOnlyRepositoryMock
{
    public Mock<IShortUrlWriteOnlyRepository> Mock { get; } = new();

    public ShortUrlWriteOnlyRepositoryMock()
    {
        Mock.Setup(r => r.Add(It.IsAny<ShortUrlEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
