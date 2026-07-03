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

    /// <summary>
    /// Initializes a new instance using the supplied System.Text.Json options.
    /// </summary>
    /// <param name="jsonSerializerOptions">The serialization options to use for all Cosmos payloads.</param>
    public SystemTextJsonCosmosSerializer(JsonSerializerOptions jsonSerializerOptions)
    {
        this._SystemTextJsonSerializer = new JsonObjectSerializer(jsonSerializerOptions);
        this._JsonSerializerOptions = jsonSerializerOptions;
    }

    /// <summary>Deserializes an object of type <typeparamref name="T"/> from the given stream.</summary>
    /// <typeparam name="T">The type to deserialize.</typeparam>
    /// <param name="stream">The stream containing the serialized payload. The stream is disposed.</param>
    /// <returns>The deserialized instance, or default if the stream is empty.</returns>
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

    /// <summary>Serializes <paramref name="input"/> to a new stream positioned at the start.</summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="input">The object to serialize.</param>
    /// <returns>A rewound stream containing the serialized payload.</returns>
    public override Stream ToStream<T>(T input)
    {
        MemoryStream streamPayload = new MemoryStream();
        this._SystemTextJsonSerializer.Serialize(streamPayload, input, input.GetType(), default);
        streamPayload.Position = 0;
        return streamPayload;
    }

    /// <summary>
    /// Resolves the serialized JSON name for a member, honoring <see cref="JsonExtensionDataAttribute"/>,
    /// <see cref="JsonPropertyNameAttribute"/>, and the configured naming policy. Used by LINQ-to-Cosmos
    /// translation.
    /// </summary>
    /// <param name="memberInfo">The member being serialized.</param>
    /// <returns>The serialized name, or null for members that carry extension data.</returns>
    public override string? SerializeMemberName(MemberInfo memberInfo)
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
