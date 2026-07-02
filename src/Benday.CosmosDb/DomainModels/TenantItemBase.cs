using Benday.Common.Interfaces;

namespace Benday.CosmosDb.DomainModels;

/// <summary>
/// Base class for an item that belongs to a tenant.
/// </summary>
public abstract class TenantItemBase : CosmosIdentityBase, ITenantItem, IBlobOwner
{
    /// <summary>
    /// Get the entity type name for this entity. This value will be the name of the class.
    /// </summary>
    /// <returns></returns>
    protected override string GetEntityTypeName()
    {
        return GetType().Name;
    }

    /// <summary>
    /// Default blob prefix: "{TenantId}/{Id}/".
    /// Override in subclasses for different path conventions.
    /// </summary>
    public virtual string GetBlobPrefix()
    {
        return $"{TenantId}/{Id}/";
    }
}
