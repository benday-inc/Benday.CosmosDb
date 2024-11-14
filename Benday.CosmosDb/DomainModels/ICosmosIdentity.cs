using Benday.CosmosDb.Repositories;

namespace Benday.CosmosDb.DomainModels;

using Benday.CosmosDb.Utilities;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the basic properties of a Cosmos DB entity.
/// </summary>
public interface ICosmosIdentity
{
    /// <summary>
    /// Id of the entity.
    /// </summary>
    [JsonPropertyName(CosmosDbConstants.PropertyName_Id)]
    string Id { get; set; }

    /// <summary>
    /// Top-level partition key of the entity.
    /// </summary>
    [JsonPropertyName(CosmosDbConstants.PropertyName_PartitionKey)]
    string PartitionKey { get; set; }

    /// <summary>
    /// Timestamp of the entity.
    /// </summary>
    [JsonPropertyName(CosmosDbConstants.PropertyName_Timestamp)]    
    long TimestampUnixStyle { get; set; }

    /// <summary>
    /// Timestamp of the entity.
    /// </summary>
    DateTime Timestamp 
    {
        get;
        set;
    }

    /// <summary>
    /// Optimistic concurrency token for the entity.
    /// </summary>
    [JsonPropertyName(CosmosDbConstants.PropertyName_Etag)]
    string Etag { get; set; }

    /// <summary>
    /// Second-level partition key for the entity.  This value describes the domain model type for the entity.
    /// </summary>
    [JsonPropertyName(CosmosDbConstants.PropertyName_Discriminator)]
    string DiscriminatorValue
    {
        get;
    }
}
