namespace Benday.Identity.CosmosDb
{
    /// <summary>
    /// Represents a login and its associated provider for a user.
    /// </summary>
    public class IdentityUserLogin
    {
        /// <summary>
        /// Gets or sets the login provider (e.g., Google, Facebook, Microsoft).
        /// </summary>
        public string LoginProvider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique identifier for this user provided by the login provider.
        /// </summary>
        public string ProviderKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the friendly name used for this login.
        /// </summary>
        public string ProviderDisplayName { get; set; } = string.Empty;
    }
}
