using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benday.CosmosDb.Utilities;

/// <summary>
/// Source: https://github.com/Azure/azure-cosmos-dotnet-v3/blob/master/Microsoft.Azure.Cosmos.Samples/Usage/SystemTextJson/CosmosSystemTextJsonSerializer.cs
/// </summary>
public class SystemTextJsonCosmosSerializer : CosmosLinqSerializer
{
    private readonly JsonObjectSerializer _SystemTextJsonSerializer;
    private readonly JsonSerializerOptions _JsonSerializerOptions;

    public SystemTextJsonCosmosSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
        this._SystemTextJsonSerializer = new JsonObjectSerializer(jsonSerializerOptions);
        this._JsonSerializerOptions = jsonSerializerOptions;
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (stream.CanSeek
                   && stream.Length == 0)
            {
                return default!;
            }

            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            var result = this._SystemTextJsonSerializer.Deserialize(stream, typeof(T), default);

            if (result is T)
            {
                return (T)result;
            }
            else
            {
                return default!;
            }
        }
    }

    public override Stream ToStream<T>(T input)
    {
        MemoryStream streamPayload = new MemoryStream();
        this._SystemTextJsonSerializer.Serialize(streamPayload, input, input.GetType(), default);
        streamPayload.Position = 0;
        return streamPayload;
    }

    public override string SerializeMemberName(MemberInfo memberInfo)
    {
        var jsonExtensionDataAttribute = memberInfo.GetCustomAttribute<JsonExtensionDataAttribute>(true);
        if (jsonExtensionDataAttribute != null)
        {
            return null;
        }

        var jsonPropertyNameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(true);
        if (!string.IsNullOrEmpty(jsonPropertyNameAttribute?.Name))
        {
            return jsonPropertyNameAttribute.Name;
        }

        if (this._JsonSerializerOptions.PropertyNamingPolicy != null)
        {
            return this._JsonSerializerOptions.PropertyNamingPolicy.ConvertName(memberInfo.Name);
        }

        // Do any additional handling of JsonSerializerOptions here.

        return memberInfo.Name;
    }
}
