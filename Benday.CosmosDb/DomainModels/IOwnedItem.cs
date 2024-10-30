namespace Benday.CosmosDb.DomainModels;

/// <summary>
/// Interface for an item that has an owner.
/// </summary>
public interface IOwnedItem : ICosmosIdentity
{
    /// <summary>
    /// Owner id of the entity. By default, this value will be the same as the PartitionKey for the entity.
    /// </summary>
    string OwnerId { get; set; }
}
