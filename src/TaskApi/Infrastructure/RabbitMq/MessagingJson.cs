using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskApi.Infrastructure.RabbitMq;

internal static class MessagingJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(), new UtcDateTimeOffsetConverter() },
    };

    public static byte[] ToUtf8Bytes(IIntegrationEvent integrationEvent) =>
        JsonSerializer.SerializeToUtf8Bytes(integrationEvent, integrationEvent.GetType(), Options);

    public static string ToJson(IIntegrationEvent integrationEvent) =>
        JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), Options);
}
