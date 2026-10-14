using System.Threading.Channels;

namespace TaskApi.Infrastructure.RabbitMq;

public class ChannelEventPublisher(
    Channel<IIntegrationEvent> channel,
    ILogger<ChannelEventPublisher> logger) : IIntegrationEventPublisher
{
    public void Publish(IIntegrationEvent integrationEvent)
    {
        if (channel.Writer.TryWrite(integrationEvent))
        {
            return;
        }

        logger.LogError(
            "Event {RoutingKey} ({EventId}) was NOT queued (queue full or shutting down). Payload: {Payload}",
            integrationEvent.RoutingKey,
            integrationEvent.EventId,
            MessagingJson.ToJson(integrationEvent));
    }
}
