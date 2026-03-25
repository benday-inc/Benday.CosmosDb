using Benday.CosmosDb.DomainModels;

namespace Benday.Identity.CosmosDb
{
    public abstract class SystemTenantItem : TenantItemBase
    {
        public override string TenantId
        {
            get => CosmosIdentityConstants.SystemTenantId;
            // Setter required to match base class signature; TenantId is always SystemTenantId for system items.
            set { }
        }
    }
}
