namespace Benday.Identity.CosmosDb
{
    public static class CosmosIdentityConstants
    {
        public static string SystemTenantId = "SYSTEM";

        /// <summary>
        /// The authorization policy name used to protect admin pages.
        /// </summary>
        public const string AdminPolicyName = "CosmosIdentityAdmin";
    }
}
