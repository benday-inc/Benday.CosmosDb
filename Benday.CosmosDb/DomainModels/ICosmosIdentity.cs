using Benday.CosmosDb.Repositories;

namespace Benday.CosmosDb.DomainModels;

using Benday.CosmosDb.Utilities;
using Newtonsoft.Json;

/// <summary>
/// Represents the basic properties of a Cosmos DB entity.
/// </summary>
public interface ICosmosIdentity
{
    /// <summary>
    /// Id of the entity.
    /// </summary>
    [JsonProperty("id")]
    string Id { get; set; }

    /// <summary>
    /// Top-level partition key of the entity.
    /// </summary>
    [JsonProperty("pk")]
    string PartitionKey { get; set; }

    /// <summary>
    /// Timestamp of the entity.
    /// </summary>
    [JsonProperty("_ts")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Optimistic concurrency token for the entity.
    /// </summary>
    [JsonProperty("_etag")]
    public string Etag { get; set; }

    /// <summary>
    /// Second-level partition key for the entity.  This value describes the domain model type for the entity.
    /// </summary>
    [JsonProperty(CosmosDbConstants.DiscriminatorPropertyName)]
    string DiscriminatorValue
    {
        get;
    }
}
