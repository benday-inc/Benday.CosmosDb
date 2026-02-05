using Benday.CosmosDb.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Benday.Identity.CosmosDb;

public class CosmosDbUserStore : CosmosOwnedItemRepository<CosmosIdentityUser>,
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
    IQueryableUserStore<CosmosIdentityUser>,
    ICosmosDbUserStore
{
    public CosmosDbUserStore(
       IOptions<CosmosRepositoryOptions<CosmosIdentityUser>> options,
       CosmosClient client, ILogger<CosmosDbUserStore> logger) :
       base(options, client, logger)
    {
    }

    #region IUserStore

    public async Task<IdentityResult> CreateAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        await SaveAsync(user);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        await DeleteAsync(user);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        await SaveAsync(user);
        return IdentityResult.Success;
    }

    public async Task<CosmosIdentityUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await GetByIdAsync(CosmosIdentityConstants.SystemOwnerId, userId);
    }

    public async Task<CosmosIdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        var query = await GetQueryable();
        var queryable = query.Queryable.Where(x => x.NormalizedUserName == normalizedUserName);
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results.FirstOrDefault();
    }

    public Task<string> GetUserIdAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string?> GetUserNameAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.UserName);
    }

    public Task SetUserNameAsync(CosmosIdentityUser user, string? userName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userName))
        {
            throw new ArgumentException($"{nameof(userName)} is null or empty.", nameof(userName));
        }

        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.NormalizedUserName);
    }

    public Task SetNormalizedUserNameAsync(CosmosIdentityUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        // no-op: normalized value is computed from UserName
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }

    #endregion

    #region IUserEmailStore

    public async Task<CosmosIdentityUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var query = await GetQueryable();
        var queryable = query.Queryable.Where(x => x.NormalizedEmail == normalizedEmail);
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results.FirstOrDefault();
    }

    public Task<string?> GetEmailAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Email);
    }

    public Task SetEmailAsync(CosmosIdentityUser user, string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(email))
        {
            throw new ArgumentException($"{nameof(email)} is null or empty.", nameof(email));
        }

        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedEmailAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(CosmosIdentityUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        // no-op: normalized value is computed from Email
        return Task.CompletedTask;
    }

    public Task<bool> GetEmailConfirmedAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.EmailConfirmed);
    }

    public Task SetEmailConfirmedAsync(CosmosIdentityUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserPasswordStore

    public Task<string?> GetPasswordHashAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.PasswordHash);
    }

    public Task SetPasswordHashAsync(CosmosIdentityUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(passwordHash))
        {
            throw new ArgumentException($"{nameof(passwordHash)} is null or empty.", nameof(passwordHash));
        }

        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<bool> HasPasswordAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
    }

    #endregion

    #region IUserRoleStore

    public async Task AddToRoleAsync(CosmosIdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        var match = user.Claims.Find(x => x.ClaimType == ClaimTypes.Role && x.ClaimValue == roleName);

        if (match == null)
        {
            user.Claims.Add(new CosmosIdentityUserClaim()
            {
                ClaimType = ClaimTypes.Role,
                ClaimValue = roleName
            });

            await SaveAsync(user);
        }
    }

    public async Task RemoveFromRoleAsync(CosmosIdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        var match = user.Claims.Find(x => x.ClaimType == ClaimTypes.Role && x.ClaimValue == roleName);

        if (match != null)
        {
            user.Claims.Remove(match);
            await SaveAsync(user);
        }
    }

    public Task<IList<string>> GetRolesAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        var roles = user.Claims.FindAll(x => x.ClaimType == ClaimTypes.Role);
        return Task.FromResult<IList<string>>(roles.Select(x => x.ClaimValue).ToList());
    }

    public Task<bool> IsInRoleAsync(CosmosIdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        var isInRole = user.Claims.Any(x => x.ClaimType == ClaimTypes.Role && x.ClaimValue == roleName);
        return Task.FromResult(isInRole);
    }

    public async Task<IList<CosmosIdentityUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var query = await GetQueryable();
        var queryable = query.Queryable.Where(x =>
            x.Claims.Any(y => y.ClaimType == ClaimTypes.Role && y.ClaimValue == roleName));
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results;
    }

    #endregion

    #region IUserSecurityStampStore

    public Task<string?> GetSecurityStampAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.SecurityStamp);
    }

    public Task SetSecurityStampAsync(CosmosIdentityUser user, string stamp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(stamp))
        {
            throw new ArgumentException($"{nameof(stamp)} is null or empty.", nameof(stamp));
        }

        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserLockoutStore

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnd);
    }

    public Task SetLockoutEndDateAsync(CosmosIdentityUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task<int> IncrementAccessFailedCountAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task<bool> GetLockoutEnabledAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnabled);
    }

    public Task SetLockoutEnabledAsync(CosmosIdentityUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserClaimStore

    public Task<IList<Claim>> GetClaimsAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        var claims = user.Claims
            .Select(c => new Claim(c.ClaimType, c.ClaimValue))
            .ToList();
        return Task.FromResult<IList<Claim>>(claims);
    }

    public Task AddClaimsAsync(CosmosIdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            var existing = user.Claims.Find(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
            if (existing == null)
            {
                user.Claims.Add(new CosmosIdentityUserClaim
                {
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });
            }
        }
        return Task.CompletedTask;
    }

    public Task ReplaceClaimAsync(CosmosIdentityUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        var matchingClaims = user.Claims.FindAll(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
        foreach (var matchingClaim in matchingClaims)
        {
            matchingClaim.ClaimType = newClaim.Type;
            matchingClaim.ClaimValue = newClaim.Value;
        }
        return Task.CompletedTask;
    }

    public Task RemoveClaimsAsync(CosmosIdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            var matchingClaims = user.Claims.FindAll(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
            foreach (var matchingClaim in matchingClaims)
            {
                user.Claims.Remove(matchingClaim);
            }
        }
        return Task.CompletedTask;
    }

    public async Task<IList<CosmosIdentityUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        var query = await GetQueryable();
        var queryable = query.Queryable.Where(x =>
            x.Claims.Any(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value));
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results;
    }

    #endregion

    #region IUserTwoFactorStore

    public Task<bool> GetTwoFactorEnabledAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.TwoFactorEnabled);
    }

    public Task SetTwoFactorEnabledAsync(CosmosIdentityUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserPhoneNumberStore

    public Task<string?> GetPhoneNumberAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumber);
    }

    public Task SetPhoneNumberAsync(CosmosIdentityUser user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    public Task<bool> GetPhoneNumberConfirmedAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    public Task SetPhoneNumberConfirmedAsync(CosmosIdentityUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.PhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserAuthenticatorKeyStore

    public Task<string?> GetAuthenticatorKeyAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.AuthenticatorKey);
    }

    public Task SetAuthenticatorKeyAsync(CosmosIdentityUser user, string key, CancellationToken cancellationToken)
    {
        user.AuthenticatorKey = key;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserTwoFactorRecoveryCodeStore

    public Task ReplaceCodesAsync(CosmosIdentityUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        user.RecoveryCodes = recoveryCodes.ToList();
        return Task.CompletedTask;
    }

    public Task<bool> RedeemCodeAsync(CosmosIdentityUser user, string code, CancellationToken cancellationToken)
    {
        if (user.RecoveryCodes.Contains(code))
        {
            user.RecoveryCodes.Remove(code);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<int> CountCodesAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.RecoveryCodes.Count);
    }

    #endregion

    #region IUserLoginStore

    public Task AddLoginAsync(CosmosIdentityUser user, UserLoginInfo login, CancellationToken cancellationToken)
    {
        var existing = user.Logins.Find(l =>
            l.LoginProvider == login.LoginProvider && l.ProviderKey == login.ProviderKey);

        if (existing == null)
        {
            user.Logins.Add(new CosmosIdentityUserLogin
            {
                LoginProvider = login.LoginProvider,
                ProviderKey = login.ProviderKey,
                ProviderDisplayName = login.ProviderDisplayName ?? string.Empty
            });
        }
        return Task.CompletedTask;
    }

    public Task RemoveLoginAsync(CosmosIdentityUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        var login = user.Logins.Find(l =>
            l.LoginProvider == loginProvider && l.ProviderKey == providerKey);

        if (login != null)
        {
            user.Logins.Remove(login);
        }
        return Task.CompletedTask;
    }

    public Task<IList<UserLoginInfo>> GetLoginsAsync(CosmosIdentityUser user, CancellationToken cancellationToken)
    {
        var logins = user.Logins
            .Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName))
            .ToList();
        return Task.FromResult<IList<UserLoginInfo>>(logins);
    }

    public async Task<CosmosIdentityUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        var query = await GetQueryable();
        var queryable = query.Queryable.Where(x =>
            x.Logins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey));
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results.FirstOrDefault();
    }

    #endregion

    #region IQueryableUserStore

    public IQueryable<CosmosIdentityUser> Users
    {
        get
        {
            var query = GetQueryable().GetAwaiter().GetResult();
            return query.Queryable;
        }
    }

    #endregion
}
