using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Benday.Identity.CosmosDb
{
    /// <summary>
    /// A default implementation of <see cref="UserClaimsPrincipalFactory{TUser, TRole}"/> that
    /// adds role claims to the claims identity. Override this class to customize the claims
    /// generation for your application.
    /// </summary>
    public class DefaultUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<CosmosIdentityUser, CosmosIdentityRole>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultUserClaimsPrincipalFactory"/> class.
        /// </summary>
        /// <param name="userManager">The user manager used to retrieve user information such as roles.</param>
        /// <param name="roleManager">The role manager used to retrieve role information.</param>
        /// <param name="optionsAccessor">The accessor for the configured identity options.</param>
        public DefaultUserClaimsPrincipalFactory(
            UserManager<CosmosIdentityUser> userManager,
            RoleManager<CosmosIdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor) { }

        /// <inheritdoc/>
        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(CosmosIdentityUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            var roles = await UserManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
            return identity;
        }
    }
}
