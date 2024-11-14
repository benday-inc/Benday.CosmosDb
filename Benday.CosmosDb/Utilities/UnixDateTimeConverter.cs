using Microsoft.Azure.Cosmos;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benday.CosmosDb.Utilities;

/// <summary>
/// Converts a Unix timestamp to a DateTime and vice versa.
/// </summary>
public class UnixDateTimeConverter : JsonConverter<DateTime>
{
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var unixTime = ((DateTimeOffset)value).ToUnixTimeSeconds();

        writer.WriteNumberValue(unixTime);
    }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var unixTimestamp = reader.GetInt64();

        var returnValue = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;

        return returnValue;
    }
}
