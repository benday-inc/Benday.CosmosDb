using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Repositories;
using System.Text.Json.Serialization;

namespace Benday.Identity.CosmosDb
{
    /// <summary>
    /// Base class for identity entities that always live in the reserved system tenant.
    /// Fixes <see cref="TenantId"/> to <see cref="CosmosIdentityConstants.SystemTenantId"/> so users, roles, and claims are not scoped to an application tenant.
    /// </summary>
    public abstract class SystemTenantItem : TenantItemBase
    {
        /// <summary>
        /// The tenant identifier for this item; always returns <see cref="CosmosIdentityConstants.SystemTenantId"/> and ignores any value set.
        /// </summary>
        [JsonPropertyName(CosmosDbConstants.PropertyName_TenantId)]
        public override string TenantId
        {
            get => CosmosIdentityConstants.SystemTenantId;
            // Setter required to match base class signature; TenantId is always SystemTenantId for system items.
            set { }
        }
    }
}
