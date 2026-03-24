namespace Benday.CosmosDb.DomainModels;

/// <summary>
/// Base class for an item that belongs to a tenant.
/// </summary>
public abstract class TenantItemBase : CosmosIdentityBase, ITenantItem
{
    /// <summary>
    /// Get the entity type name for this entity. This value will be the name of the class.
    /// </summary>
    /// <returns></returns>
    protected override string GetEntityTypeName()
    {
        return GetType().Name;
    }
}
