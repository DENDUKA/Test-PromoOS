namespace TaskApi.Application.Abstractions;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    string RoutingKey { get; }
}

public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Ставит событие в очередь на отправку и сразу возвращает управление.
    /// Не бросает исключений и не ждёт брокер: недоступность RabbitMQ не должна ломать запрос.
    /// </summary>
    void Publish(IIntegrationEvent integrationEvent);
}
