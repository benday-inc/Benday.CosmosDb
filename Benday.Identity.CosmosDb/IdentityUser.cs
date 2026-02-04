namespace Benday.Identity.CosmosDb
{
    public class IdentityUser : SystemOwnedItem
    {
        public string UserName { get; set; } = string.Empty;
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
        public string Email { get; set; } = string.Empty;
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
        public bool EmailConfirmed { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
        public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        // Phone number properties
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }

        // Two-factor authentication properties
        public bool TwoFactorEnabled { get; set; }
        public string? AuthenticatorKey { get; set; }
        public List<string> RecoveryCodes { get; set; } = new List<string>();

        // Lockout properties
        public bool LockoutEnabled { get; set; } = true;
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }

        // Claims and roles
        public List<IdentityUserClaim> Claims { get; set; } = new List<IdentityUserClaim>();

        // External logins
        public List<IdentityUserLogin> Logins { get; set; } = new List<IdentityUserLogin>();
    }
}
