namespace Benday.CosmosDb.DomainModels;

/// <summary>
/// A tenant-scoped entity that belongs to a parent entity. Adds a Cosmos-specific
/// parent discriminator on top of the shared parented-item and tenant-item contracts.
/// </summary>
public interface IParentedItem
    : ITenantItem,
      Benday.Common.Interfaces.IParentedItem<string>
{
    /// <summary>
    /// Entity type of the parent entity. Cosmos-specific discriminator.
    /// </summary>
    string ParentEntityType { get; set; }
}
