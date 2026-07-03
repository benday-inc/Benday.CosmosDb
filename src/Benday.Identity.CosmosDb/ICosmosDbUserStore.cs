using Benday.CosmosDb.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Benday.Identity.CosmosDb;

/// <summary>
/// Cosmos DB user store abstraction that aggregates the ASP.NET Core Identity user
/// store interfaces (password, email, roles, claims, two-factor, lockout, logins,
/// passkeys, and queryable access) with the tenant-aware repository contract.
/// </summary>
public interface ICosmosDbUserStore : ITenantItemRepository<CosmosIdentityUser>,
    IUserStore<CosmosIdentityUser>,
    IUserPasswordStore<CosmosIdentityUser>,
    IUserEmailStore<CosmosIdentityUser>,
    IUserRoleStore<CosmosIdentityUser>,
    IUserSecurityStampStore<CosmosIdentityUser>,
    IUserLockoutStore<CosmosIdentityUser>,
    IUserClaimStore<CosmosIdentityUser>,
    IUserTwoFactorStore<CosmosIdentityUser>,
    IUserPhoneNumberStore<CosmosIdentityUser>,
    IUserAuthenticatorKeyStore<CosmosIdentityUser>,
    IUserTwoFactorRecoveryCodeStore<CosmosIdentityUser>,
    IUserLoginStore<CosmosIdentityUser>,
    IUserPasskeyStore<CosmosIdentityUser>,
    IQueryableUserStore<CosmosIdentityUser>
{
}
