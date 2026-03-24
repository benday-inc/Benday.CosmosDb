using Benday.CosmosDb.DomainModels;

namespace Benday.Identity.CosmosDb
{
    public abstract class SystemTenantItem : TenantItemBase
    {
        public override string TenantId { get => CosmosIdentityConstants.SystemTenantId; }
    }
}
