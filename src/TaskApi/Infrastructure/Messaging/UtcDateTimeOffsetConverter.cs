using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskApi.Infrastructure.Messaging;

/// <summary>
/// System.Text.Json пишет DateTimeOffset с нулевым смещением как "+00:00", а контракт (см. задание)
/// требует "2026-05-07T12:00:00Z". Для потребителей это один и тот же момент, но формат стоит держать как в ТЗ.
/// </summary>
internal sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDateTimeOffset();

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture));
}
