namespace TaskApi.Infrastructure.RabbitMq;

public interface IIntegrationEventPublisher
{
    void Publish(IIntegrationEvent integrationEvent);
}
