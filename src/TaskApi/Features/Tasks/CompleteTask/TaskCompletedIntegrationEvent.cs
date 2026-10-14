using System.Text.Json.Serialization;
using TaskApi.Domain.Tasks;
using TaskApi.Infrastructure.RabbitMq;

namespace TaskApi.Features.Tasks.CompleteTask;

public record TaskCompletedIntegrationEvent(
    Guid TaskId,
    string Title,
    DateTimeOffset CompletedAt,
    Priority Priority) : IIntegrationEvent
{
    public const string Key = "task.completed";

    [JsonIgnore]
    public Guid EventId { get; } = Guid.NewGuid();

    [JsonIgnore]
    public string RoutingKey => Key;
}
