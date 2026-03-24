namespace Benday.CosmosDb.DomainModels;

public interface IParentedItem : ITenantItem
{
    /// <summary>
    /// Parent ID in our system
    /// </summary>
    string ParentId { get; set; }

    /// <summary>
    /// Entity type of the parent entity
    /// </summary>
    string ParentEntityType { get; set; }
}
