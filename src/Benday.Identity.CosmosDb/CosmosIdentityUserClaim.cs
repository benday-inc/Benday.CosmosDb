namespace Benday.Identity.CosmosDb
{
    /// <summary>
    /// A single claim (type/value pair) assigned to a <see cref="CosmosIdentityUser"/>.
    /// </summary>
    public class CosmosIdentityUserClaim
    {
        /// <summary>
        /// The claim type, such as a role or permission identifier.
        /// </summary>
        public string ClaimType { get; set; } = string.Empty;
        /// <summary>
        /// The value associated with the claim type.
        /// </summary>
        public string ClaimValue { get; set; } = string.Empty;
    }
}
