using Benday.CosmosDb.DomainModels;

namespace Benday.Identity.CosmosDb
{
    public abstract class SystemOwnedItem : OwnedItemBase
    {
        public override string OwnerId { get => IdentityConstants.SystemOwnerId; }
    }
}
