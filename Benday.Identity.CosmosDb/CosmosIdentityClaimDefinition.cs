namespace Benday.Identity.CosmosDb
{
    /// <summary>
    /// Defines an available claim type that can be assigned to users.
    /// Stored in Cosmos DB and used by admin screens to provide a
    /// consistent list of claim types and optional allowed values.
    /// </summary>
    public class CosmosIdentityClaimDefinition : SystemOwnedItem
    {
        /// <summary>
        /// The claim type name (e.g., "Department", "CanExport").
        /// </summary>
        public string ClaimType { get; set; } = string.Empty;

        /// <summary>
        /// Optional description shown in admin UI.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Optional list of allowed values for this claim type.
        /// When empty, any free-text value is allowed.
        /// </summary>
        public List<string> AllowedValues { get; set; } = new List<string>();
    }
}
