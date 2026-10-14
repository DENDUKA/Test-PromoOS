using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskApi.Infrastructure.Messaging;

internal static class MessagingJson
{
    // Контракт сообщения: camelCase, enum строкой ("High"), время в UTC с суффиксом Z.
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(), new UtcDateTimeOffsetConverter() },
    };

    public static byte[] ToUtf8Bytes(IIntegrationEvent integrationEvent) =>
        JsonSerializer.SerializeToUtf8Bytes(integrationEvent, integrationEvent.GetType(), Options);

    public static string ToJson(IIntegrationEvent integrationEvent) =>
        JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), Options);
}
