namespace Benday.Identity.CosmosDb
{
    /// <summary>
    /// Cosmos DB-backed ASP.NET Core Identity role entity, representing a named role and its associated claims.
    /// </summary>
    public class CosmosIdentityRole : SystemTenantItem
    {
        /// <summary>
        /// The display name of the role (for example, "Admin").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Upper-cased form of <see cref="Name"/> used for case-insensitive lookups; computed on read and ignored on write.
        /// </summary>
        public string NormalizedName
        {
            get
            {
                return Name.ToUpperInvariant();
            }
            set
            {
                // no-op: normalized value is computed from Name
            }
        }

        /// <summary>
        /// A random value that changes on each save, used for optimistic concurrency checks.
        /// </summary>
        public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The claims granted to every user who belongs to this role.
        /// </summary>
        public List<CosmosIdentityClaim> Claims { get; set; } = new List<CosmosIdentityClaim>();
    }
}
