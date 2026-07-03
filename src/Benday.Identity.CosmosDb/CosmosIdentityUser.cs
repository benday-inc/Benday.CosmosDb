namespace Benday.Identity.CosmosDb
{
    /// <summary>
    /// Cosmos DB-backed ASP.NET Core Identity user entity. Holds credential, profile,
    /// two-factor, lockout, claim, external-login, and passkey data for an authenticated user.
    /// </summary>
    public class CosmosIdentityUser : SystemTenantItem
    {
        /// <summary>
        /// The user's given (first) name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;
        /// <summary>
        /// The user's family (last) name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// The login name that identifies this user when signing in.
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        /// <summary>
        /// Upper-cased form of <see cref="UserName"/> used for case-insensitive lookups; computed on read and ignored on write.
        /// </summary>
        public string NormalizedUserName
        {
            get
            {
                return UserName.ToUpperInvariant();
            }
            set
            {
                // no-op: normalized value is computed from UserName
            }
        }
        /// <summary>
        /// The user's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>
        /// Upper-cased form of <see cref="Email"/> used for case-insensitive lookups; computed on read and ignored on write.
        /// </summary>
        public string NormalizedEmail
        {
            get
            {
                return Email.ToUpperInvariant();
            }
            set
            {
                // no-op: normalized value is computed from Email
            }
        }
        /// <summary>
        /// Indicates whether the user has confirmed ownership of their email address.
        /// </summary>
        public bool EmailConfirmed { get; set; }
        /// <summary>
        /// The salted and hashed representation of the user's password.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;
        /// <summary>
        /// A value that changes whenever the user's security-sensitive credentials change, used to invalidate outstanding sessions and tokens.
        /// </summary>
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// A random value that changes on each save, used for optimistic concurrency checks.
        /// </summary>
        public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        // Phone number properties
        /// <summary>
        /// The user's telephone number, if provided.
        /// </summary>
        public string? PhoneNumber { get; set; }
        /// <summary>
        /// Indicates whether the user has confirmed ownership of their phone number.
        /// </summary>
        public bool PhoneNumberConfirmed { get; set; }

        // Two-factor authentication properties
        /// <summary>
        /// Indicates whether two-factor authentication is enabled for the user.
        /// </summary>
        public bool TwoFactorEnabled { get; set; }
        /// <summary>
        /// The shared secret key used by an authenticator app to generate TOTP codes for this user.
        /// </summary>
        public string? AuthenticatorKey { get; set; }
        /// <summary>
        /// Single-use recovery codes the user can enter in place of a two-factor code.
        /// </summary>
        public List<string> RecoveryCodes { get; set; } = new List<string>();

        // Lockout properties
        /// <summary>
        /// Indicates whether the account can be locked out after repeated failed sign-in attempts.
        /// </summary>
        public bool LockoutEnabled { get; set; } = true;
        /// <summary>
        /// The date and time, if any, until which the account is locked out.
        /// </summary>
        public DateTimeOffset? LockoutEnd { get; set; }
        /// <summary>
        /// The number of consecutive failed sign-in attempts since the last successful sign-in.
        /// </summary>
        public int AccessFailedCount { get; set; }

        // Claims and roles
        /// <summary>
        /// The claims assigned to this user, including role membership.
        /// </summary>
        public List<CosmosIdentityUserClaim> Claims { get; set; } = new List<CosmosIdentityUserClaim>();

        // External logins
        /// <summary>
        /// External (third-party) login providers linked to this user.
        /// </summary>
        public List<CosmosIdentityUserLogin> Logins { get; set; } = new List<CosmosIdentityUserLogin>();

        // Passkeys (WebAuthn/FIDO2)
        /// <summary>
        /// The WebAuthn/FIDO2 passkey credentials registered by this user.
        /// </summary>
        public List<CosmosIdentityUserPasskey> Passkeys { get; set; } = new List<CosmosIdentityUserPasskey>();
    }
}
