using System.Security.Claims;

namespace Benday.Identity.CosmosDb
{
    public static class CosmosIdentityRoleExtensions
    {
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
