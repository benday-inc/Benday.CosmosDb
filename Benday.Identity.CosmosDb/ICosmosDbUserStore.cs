using Benday.CosmosDb.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Benday.Identity.CosmosDb;

public interface ICosmosDbUserStore : IOwnedItemRepository<CosmosIdentityUser>,
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
    IQueryableUserStore<CosmosIdentityUser>
{
}
