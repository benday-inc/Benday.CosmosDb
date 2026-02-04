using Benday.CosmosDb.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Benday.Identity.CosmosDb;

public interface ICosmosDbUserStore : IOwnedItemRepository<IdentityUser>,
    IUserStore<IdentityUser>,
    IUserPasswordStore<IdentityUser>,
    IUserEmailStore<IdentityUser>,
    IUserRoleStore<IdentityUser>,
    IUserSecurityStampStore<IdentityUser>,
    IUserLockoutStore<IdentityUser>,
    IUserClaimStore<IdentityUser>,
    IUserTwoFactorStore<IdentityUser>,
    IUserPhoneNumberStore<IdentityUser>,
    IUserAuthenticatorKeyStore<IdentityUser>,
    IUserTwoFactorRecoveryCodeStore<IdentityUser>,
    IUserLoginStore<IdentityUser>,
    IQueryableUserStore<IdentityUser>
{
}
