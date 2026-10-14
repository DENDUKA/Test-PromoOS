using System.Threading.Channels;

namespace TaskApi.Infrastructure.Messaging;

/// <summary>
/// Неблокирующий публикатор: кладёт событие в память и возвращается. Реальная отправка в RabbitMQ
/// идёт в <see cref="RabbitMqPublisherBackgroundService"/>, поэтому HTTP-запрос не ждёт брокер.
/// </summary>
public sealed class ChannelEventPublisher(
    Channel<IIntegrationEvent> channel,
    ILogger<ChannelEventPublisher> logger) : IIntegrationEventPublisher
{
    public void Publish(IIntegrationEvent integrationEvent)
    {
        // TryWrite не блокирует: false = очередь переполнена или уже закрыта (идёт остановка приложения).
        if (channel.Writer.TryWrite(integrationEvent))
        {
            return;
        }

        // Fail silently, но с логом: задача уже завершена в БД, а событие мы потеряли.
        logger.LogError(
            "Event {RoutingKey} ({EventId}) was NOT queued (queue full or shutting down). Payload: {Payload}",
            integrationEvent.RoutingKey,
            integrationEvent.EventId,
            MessagingJson.ToJson(integrationEvent));
    }
}
