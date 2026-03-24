using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Benday.CosmosDb.DomainModels;

/// <summary>
/// Provides the implementation of the basic properties of a Cosmos DB entity.
/// </summary>
public abstract class CosmosIdentityBase : ITenantItem
{
    /// <summary>
    /// Id of the entity.
    /// </summary>
    [JsonPropertyName(CosmosDbConstants.PropertyName_Id)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Top-level partition key of the entity. Represents the tenant that owns this entity.
    /// </summary>
    [JsonPropertyName(CosmosDbConstants.PropertyName_TenantId)]
    public virtual string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the entity.
    /// </summary>
    [JsonPropertyName(CosmosDbConstants.PropertyName_Timestamp)]
    public long TimestampUnixStyle { get; set; }

    /// <summary>
    /// Timestamp of the entity in human-readable format.
    /// </summary>
    public DateTime Timestamp
    {
        get
        {
            return DateTimeOffset.FromUnixTimeSeconds(TimestampUnixStyle).UtcDateTime;
        }
        set
        {
        }
    }

    /// <summary>
    /// Optimistic concurrency token for the entity.
    /// </summary>
    [JsonPropertyName(CosmosDbConstants.PropertyName_Etag)]
    public string Etag { get; set; } = string.Empty;

    /// <summary>
    /// Second-level partition key for the entity. This value describes the domain model type for the entity.
    /// </summary>
    [JsonPropertyName(CosmosDbConstants.PropertyName_EntityType)]
    public virtual string EntityType
    {
        get
        {
            return GetEntityTypeName();
        }
        set
        {

        }
    }

    /// <summary>
    /// Get the entity type name for this entity.
    /// </summary>
    /// <returns>Entity type name</returns>
    /// <example>Recommendation: this should be the class name for the domain model class</example>
    protected abstract string GetEntityTypeName();
}
