namespace Benday.Identity.CosmosDb
{
    /// <summary>
    /// Well-known constant values used by the Cosmos DB identity implementation.
    /// </summary>
    public static class CosmosIdentityConstants
    {
        /// <summary>
        /// The reserved tenant identifier under which all identity entities (users, roles, claims) are stored.
        /// </summary>
        public static string SystemTenantId = "SYSTEM";

        /// <summary>
        /// The authorization policy name used to protect admin pages.
        /// </summary>
        public const string AdminPolicyName = "CosmosIdentityAdmin";
    }
}
