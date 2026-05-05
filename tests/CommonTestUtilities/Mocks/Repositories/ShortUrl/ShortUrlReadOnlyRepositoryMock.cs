using Minify.Domain.Entities;
using Minify.Domain.Repositories.ShortUrl;
using Moq;

namespace CommonTestUtilities.Mocks.Repositories.ShortUrl;

public class ShortUrlReadOnlyRepositoryMock
{
    public Mock<IShortUrlReadOnlyRepository> Mock { get; } = new();

    public void SetupFound(ShortUrlEntity entity) =>
        Mock.Setup(r => r.GetByShortCode(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

    public void SetupNotFound() =>
        Mock.Setup(r => r.GetByShortCode(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShortUrlEntity?)null);
}
