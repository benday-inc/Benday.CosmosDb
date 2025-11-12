namespace Benday.CosmosDb.DomainModels;

public interface IParentedItem : IOwnedItem
{
    /// <summary>
    /// Parent ID in our system
    /// </summary>
    string ParentId { get; set; }

    /// <summary>
    /// Discriminator/type of the parent entity
    /// </summary>
    string ParentDiscriminator { get; set; }
}