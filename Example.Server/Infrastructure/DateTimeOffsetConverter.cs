namespace Example.Server.Infrastructure;

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (String.IsNullOrEmpty(value))
        {
            return default;
        }

        try
        {
            return DateTime.Parse(value, CultureInfo.InvariantCulture);
        }
        catch
        {
            throw new FormatException();
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
    }
}
