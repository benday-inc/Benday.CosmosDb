using Benday.CosmosDb.Repositories;
using System.Text.Json.Serialization;

namespace Benday.CosmosDb.DomainModels;

/// <summary>
/// Base class for an item that has an owner.  By default, the PartitionKey will be the same as the OwnerId.
/// </summary>
public abstract class OwnedItemBase : CosmosIdentityBase, IOwnedItem
{
    /// <summary>
    /// Returns the partition key for the entity. This value will be the same as the OwnerId.
    /// </summary>
    [JsonPropertyName(CosmosDbConstants.PropertyName_PartitionKey)]
    public override string PartitionKey { get => OwnerId; set => OwnerId = value; }

    /// <summary>
    /// Get the discriminator value for the entity. This value will be the name of the class.
    /// </summary>
    /// <returns></returns>
    protected override string GetDiscriminatorName()
    {
        return GetType().Name;
    }
}
