namespace Benday.CosmosDb.DomainModels;

/// <summary>
/// Base class for an item that has both an owner and a parent entity.
/// Inherits from OwnedItemBase and adds ParentId and ParentDiscriminator properties.
/// </summary>
public abstract class ParentedItemBase : OwnedItemBase, IParentedItem
{
    /// <summary>
    /// Parent ID in our system
    /// </summary>
    public string ParentId { get; set; } = string.Empty;

    /// <summary>
    /// Discriminator/type of the parent entity
    /// </summary>
    public abstract string ParentDiscriminator { get; set; }
}
