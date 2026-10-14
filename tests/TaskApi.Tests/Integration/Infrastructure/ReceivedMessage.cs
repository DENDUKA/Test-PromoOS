using System.Text.Json;

namespace TaskApi.Tests.Integration.Infrastructure;

public record ReceivedMessage(string RoutingKey, string Body, string? ContentType, string? MessageId, bool IsPersistent)
{
    public Guid TaskId => JsonDocument.Parse(Body).RootElement.GetProperty("taskId").GetGuid();
}
