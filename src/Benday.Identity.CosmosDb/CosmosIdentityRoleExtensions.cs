using System.Security.Claims;

namespace Benday.Identity.CosmosDb
{
    /// <summary>
    /// Extension methods for working with <see cref="CosmosIdentityRole"/> and its claims.
    /// </summary>
    public static class CosmosIdentityRoleExtensions
    {
        /// <summary>
        /// Adds a claim to the role if an equivalent claim (same type and value) is not already present.
        /// </summary>
        /// <param name="role">The role to add the claim to.</param>
        /// <param name="claim">The claim to add.</param>
        public static void AddClaim(this CosmosIdentityRole role, Claim claim)
        {
            var roleClaim = new CosmosIdentityClaim
            {
                Type = claim.Type,
                Value = claim.Value
            };

            // verify claim doesn't already exist
            foreach (var item in role.Claims)
            {
                if (item.Type == roleClaim.Type &&
                    item.Value == roleClaim.Value)
                {
                    return;
                }
            }

            role.Claims.Add(roleClaim);
        }

        /// <summary>
        /// Converts a list of <see cref="CosmosIdentityClaim"/> instances into a list of <see cref="Claim"/> instances.
        /// </summary>
        /// <param name="fromClaims">The identity claims to convert.</param>
        /// <returns>A list of <see cref="Claim"/> instances created from the supplied identity claims.</returns>
        public static List<Claim> ToClaimList(this List<CosmosIdentityClaim> fromClaims)
        {
            var claims = new List<Claim>();

            foreach (var item in fromClaims)
            {
                claims.Add(new Claim(item.Type, item.Value));
            }

            return claims;
        }
    }
}
