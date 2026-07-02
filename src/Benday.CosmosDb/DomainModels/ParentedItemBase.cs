namespace Benday.CosmosDb.DomainModels;

/// <summary>
/// Base class for an item that has both a tenant and a parent entity.
/// Inherits from TenantItemBase and adds ParentId and ParentEntityType properties.
/// </summary>
public abstract class ParentedItemBase : TenantItemBase, IParentedItem
{
    /// <summary>
    /// Parent ID in our system
    /// </summary>
    public string ParentId { get; set; } = string.Empty;

    /// <summary>
    /// Entity type of the parent entity
    /// </summary>
    public abstract string ParentEntityType { get; set; }
}
