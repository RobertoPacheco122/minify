using Minify.Messaging.Events;
using Minify.Messaging.Publishers;
using Moq;

namespace CommonTestUtilities.Mocks.Publishers;

public class EventPublisherMock
{
    public Mock<IEventPublisher> Mock { get; } = new();

    public EventPublisherMock()
    {
        Mock.Setup(p => p.PublishAsync(It.IsAny<UrlClickedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public void SetupThrows(Exception exception) =>
        Mock.Setup(p => p.PublishAsync(It.IsAny<UrlClickedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
}
