namespace TaskApi.Infrastructure.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    string RoutingKey { get; }
}
