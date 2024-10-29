namespace Benday.CosmosDb.DomainModels;

public interface IOwnedItem : ICosmosIdentity
{
    string OwnerId { get; set; }
}
