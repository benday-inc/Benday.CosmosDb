using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Benday.CosmosDb.DomainModels;

/// <summary>
/// Provides the implementation of the basic properties of a Cosmos DB entity.
/// </summary>
public abstract class CosmosIdentityBase : IOwnedItem
{
    /// <summary>
    /// Id of the entity.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Top-level partition key of the entity.
    /// </summary>
    [JsonPropertyName("pk")]
    public abstract string PartitionKey { get; set; }

    /// <summary>
    /// The owner id of the entity. By default, this value will be the same as the PartitionKey for the entity.
    /// </summary>
    [JsonIgnore]
    public virtual string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Get the timestamp of the entity as a DateTime object.
    /// </summary>
    [JsonPropertyName("_ts")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Optimistic concurrency token for the entity.
    /// </summary>
    [JsonPropertyName("_etag")]
    public string Etag { get; set; } = string.Empty;

    /// <summary>
    /// Second-level partition key for the entity.  This value describes the domain model type for the entity.
    /// </summary>
    [JsonPropertyName(CosmosDbConstants.DiscriminatorPropertyName)]
    public virtual string DiscriminatorValue
    {
        get
        {
            return GetDiscriminatorName();
        }
        set
        {

        }
    }

    /// <summary>
    /// Get the discriminator value for the entity. 
    /// </summary>
    /// <returns>Discriminator value</returns>
    /// <example>Recommendation: this should be the class name for the domain model class</example>
    protected abstract string GetDiscriminatorName();
}
