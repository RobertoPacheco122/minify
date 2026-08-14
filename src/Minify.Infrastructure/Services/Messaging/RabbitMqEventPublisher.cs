using MassTransit;
using Minify.Messaging.Publishers;

namespace Minify.Infrastructure.Services.Messaging;

public class RabbitMqEventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        await publishEndpoint.Publish(message, cancellationToken);
    }
}
