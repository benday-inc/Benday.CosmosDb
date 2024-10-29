using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Benday.CosmosDb.Utilities;

public class UnixDateTimeConverter : JsonConverter
{
    public override void WriteJson(
        JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is DateTime dateTime)
        {
            var unixTime = ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
            writer.WriteValue(unixTime);
        }
    }

    public override object ReadJson(
        JsonReader reader, Type objectType, object? existingValue,
        JsonSerializer serializer)
    {
        var unixTimestamp = Convert.ToInt64(reader.Value);
        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DateTime);
    }
}
