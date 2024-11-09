using Microsoft.Azure.Cosmos;
using System.Text.Json;

namespace Benday.CosmosDb.Utilities;

public class SystemTextJsonCosmosSerializer : CosmosSerializer
{
    private readonly JsonSerializerOptions _JsonSerializerOptions;

    public SystemTextJsonCosmosSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
        _JsonSerializerOptions = jsonSerializerOptions ?? new JsonSerializerOptions();
    }

    public override T FromStream<T>(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (stream.CanSeek && stream.Length == 0) return default!;

        using (stream)
        {
            return JsonSerializer.Deserialize<T>(stream, _JsonSerializerOptions)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, input, _JsonSerializerOptions);
        stream.Position = 0;
        return stream;
    }
}
