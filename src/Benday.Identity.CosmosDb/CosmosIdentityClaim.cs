namespace Benday.Identity.CosmosDb
{
    /// <summary>
    /// A claim (type/value pair) associated with a role, persisted as a Cosmos DB system-tenant item.
    /// </summary>
    public class CosmosIdentityClaim : SystemTenantItem
    {
        /// <summary>
        /// The claim type, such as a permission or role identifier.
        /// </summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>
        /// The value associated with the claim type.
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
