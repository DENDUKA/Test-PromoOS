namespace TaskApi.Infrastructure.RabbitMq;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    string RoutingKey { get; }
}
