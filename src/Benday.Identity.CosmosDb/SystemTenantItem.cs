using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Repositories;
using System.Text.Json.Serialization;

namespace Benday.Identity.CosmosDb
{
    public abstract class SystemTenantItem : TenantItemBase
    {
        [JsonPropertyName(CosmosDbConstants.PropertyName_TenantId)]
        public override string TenantId
        {
            get => CosmosIdentityConstants.SystemTenantId;
            // Setter required to match base class signature; TenantId is always SystemTenantId for system items.
            set { }
        }
    }
}
