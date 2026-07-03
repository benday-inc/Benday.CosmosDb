using Microsoft.Azure.Cosmos;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benday.CosmosDb.Utilities;

/// <summary>
/// Converts a Unix timestamp to a DateTime and vice versa.
/// </summary>
public class UnixDateTimeConverter : JsonConverter<DateTime>
{
    /// <summary>Writes a <see cref="DateTime"/> as a Unix timestamp (seconds since the epoch).</summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">Serializer options (unused).</param>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var unixTime = ((DateTimeOffset)value).ToUnixTimeSeconds();

        writer.WriteNumberValue(unixTime);
    }

    /// <summary>Reads a Unix timestamp (seconds since the epoch) into a UTC <see cref="DateTime"/>.</summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type being converted (unused).</param>
    /// <param name="options">Serializer options (unused).</param>
    /// <returns>The converted UTC date/time.</returns>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var unixTimestamp = reader.GetInt64();

        var returnValue = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;

        return returnValue;
    }
}
