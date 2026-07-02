namespace Benday.CosmosDb.DomainModels;

public interface IParentedItem
    : ITenantItem,
      Benday.Common.Interfaces.IParentedItem<string>
{
    /// <summary>
    /// Entity type of the parent entity. Cosmos-specific discriminator.
    /// </summary>
    string ParentEntityType { get; set; }
}
