namespace Benday.CosmosDb.DomainModels;

public abstract class OwnedItemBase : CosmosIdentityBase, IOwnedItem
{
    public override string PartitionKey { get => OwnerId; set => OwnerId = value; }

    protected override string GetDiscriminatorName()
    {
        return GetType().Name;
    }
}
