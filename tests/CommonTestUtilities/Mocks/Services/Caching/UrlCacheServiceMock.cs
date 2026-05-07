using Minify.Application.Services.Caching;
using Moq;

namespace CommonTestUtilities.Mocks.Services.Caching;

public class UrlCacheServiceMock
{
    public Mock<IUrlCacheService> Mock { get; } = new();

    public UrlCacheServiceMock()
    {
        Mock.Setup(c => c.Set(It.IsAny<string>(), It.IsAny<CachedUrl>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public void SetupFound(CachedUrl cachedUrl) =>
        Mock.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedUrl);

    public void SetupNotFound() =>
        Mock.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CachedUrl?)null);
}
