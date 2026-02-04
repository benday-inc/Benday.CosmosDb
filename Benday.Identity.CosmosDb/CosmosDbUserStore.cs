using Benday.CosmosDb.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Benday.Identity.CosmosDb;

public class CosmosDbUserStore : CosmosOwnedItemRepository<IdentityUser>,
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
    IQueryableUserStore<IdentityUser>,
    ICosmosDbUserStore
{
    public CosmosDbUserStore(
       IOptions<CosmosRepositoryOptions<IdentityUser>> options,
       CosmosClient client, ILogger<CosmosDbUserStore> logger) :
       base(options, client, logger)
    {
    }

    #region IUserStore

    public async Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        await SaveAsync(user);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        await DeleteAsync(user);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        await SaveAsync(user);
        return IdentityResult.Success;
    }

    public async Task<IdentityUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await GetByIdAsync(IdentityConstants.SystemOwnerId, userId);
    }

    public async Task<IdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        var query = await GetQueryable();
        var queryable = query.Queryable.Where(x => x.NormalizedUserName == normalizedUserName);
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results.FirstOrDefault();
    }

    public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string?> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.UserName);
    }

    public Task SetUserNameAsync(IdentityUser user, string? userName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userName))
        {
            throw new ArgumentException($"{nameof(userName)} is null or empty.", nameof(userName));
        }

        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.NormalizedUserName);
    }

    public Task SetNormalizedUserNameAsync(IdentityUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        // no-op: normalized value is computed from UserName
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }

    #endregion

    #region IUserEmailStore

    public async Task<IdentityUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var query = await GetQueryable();
        var queryable = query.Queryable.Where(x => x.NormalizedEmail == normalizedEmail);
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results.FirstOrDefault();
    }

    public Task<string?> GetEmailAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.Email);
    }

    public Task SetEmailAsync(IdentityUser user, string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(email))
        {
            throw new ArgumentException($"{nameof(email)} is null or empty.", nameof(email));
        }

        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedEmailAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(IdentityUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        // no-op: normalized value is computed from Email
        return Task.CompletedTask;
    }

    public Task<bool> GetEmailConfirmedAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.EmailConfirmed);
    }

    public Task SetEmailConfirmedAsync(IdentityUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserPasswordStore

    public Task<string?> GetPasswordHashAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.PasswordHash);
    }

    public Task SetPasswordHashAsync(IdentityUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(passwordHash))
        {
            throw new ArgumentException($"{nameof(passwordHash)} is null or empty.", nameof(passwordHash));
        }

        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
    }

    #endregion

    #region IUserRoleStore

    public async Task AddToRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        var match = user.Claims.Find(x => x.ClaimType == ClaimTypes.Role && x.ClaimValue == roleName);

        if (match == null)
        {
            user.Claims.Add(new IdentityUserClaim()
            {
                ClaimType = ClaimTypes.Role,
                ClaimValue = roleName
            });

            await SaveAsync(user);
        }
    }

    public async Task RemoveFromRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        var match = user.Claims.Find(x => x.ClaimType == ClaimTypes.Role && x.ClaimValue == roleName);

        if (match != null)
        {
            user.Claims.Remove(match);
            await SaveAsync(user);
        }
    }

    public Task<IList<string>> GetRolesAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var roles = user.Claims.FindAll(x => x.ClaimType == ClaimTypes.Role);
        return Task.FromResult<IList<string>>(roles.Select(x => x.ClaimValue).ToList());
    }

    public Task<bool> IsInRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        var isInRole = user.Claims.Any(x => x.ClaimType == ClaimTypes.Role && x.ClaimValue == roleName);
        return Task.FromResult(isInRole);
    }

    public async Task<IList<IdentityUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var query = await GetQueryable();
        var queryable = query.Queryable.Where(x =>
            x.Claims.Any(y => y.ClaimType == ClaimTypes.Role && y.ClaimValue == roleName));
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results;
    }

    #endregion

    #region IUserSecurityStampStore

    public Task<string?> GetSecurityStampAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult<string?>(user.SecurityStamp);
    }

    public Task SetSecurityStampAsync(IdentityUser user, string stamp, CancellationToken cancellationToken)
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

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnd);
    }

    public Task SetLockoutEndDateAsync(IdentityUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task<int> IncrementAccessFailedCountAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task<bool> GetLockoutEnabledAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.LockoutEnabled);
    }

    public Task SetLockoutEnabledAsync(IdentityUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserClaimStore

    public Task<IList<Claim>> GetClaimsAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var claims = user.Claims
            .Select(c => new Claim(c.ClaimType, c.ClaimValue))
            .ToList();
        return Task.FromResult<IList<Claim>>(claims);
    }

    public Task AddClaimsAsync(IdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            var existing = user.Claims.Find(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
            if (existing == null)
            {
                user.Claims.Add(new IdentityUserClaim
                {
                    ClaimType = claim.Type,
                    ClaimValue = claim.Value
                });
            }
        }
        return Task.CompletedTask;
    }

    public Task ReplaceClaimAsync(IdentityUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        var matchingClaims = user.Claims.FindAll(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
        foreach (var matchingClaim in matchingClaims)
        {
            matchingClaim.ClaimType = newClaim.Type;
            matchingClaim.ClaimValue = newClaim.Value;
        }
        return Task.CompletedTask;
    }

    public Task RemoveClaimsAsync(IdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
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

    public async Task<IList<IdentityUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        var query = await GetQueryable();
        var queryable = query.Queryable.Where(x =>
            x.Claims.Any(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value));
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results;
    }

    #endregion

    #region IUserTwoFactorStore

    public Task<bool> GetTwoFactorEnabledAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.TwoFactorEnabled);
    }

    public Task SetTwoFactorEnabledAsync(IdentityUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.TwoFactorEnabled = enabled;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserPhoneNumberStore

    public Task<string?> GetPhoneNumberAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumber);
    }

    public Task SetPhoneNumberAsync(IdentityUser user, string? phoneNumber, CancellationToken cancellationToken)
    {
        user.PhoneNumber = phoneNumber;
        return Task.CompletedTask;
    }

    public Task<bool> GetPhoneNumberConfirmedAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PhoneNumberConfirmed);
    }

    public Task SetPhoneNumberConfirmedAsync(IdentityUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.PhoneNumberConfirmed = confirmed;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserAuthenticatorKeyStore

    public Task<string?> GetAuthenticatorKeyAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.AuthenticatorKey);
    }

    public Task SetAuthenticatorKeyAsync(IdentityUser user, string key, CancellationToken cancellationToken)
    {
        user.AuthenticatorKey = key;
        return Task.CompletedTask;
    }

    #endregion

    #region IUserTwoFactorRecoveryCodeStore

    public Task ReplaceCodesAsync(IdentityUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        user.RecoveryCodes = recoveryCodes.ToList();
        return Task.CompletedTask;
    }

    public Task<bool> RedeemCodeAsync(IdentityUser user, string code, CancellationToken cancellationToken)
    {
        if (user.RecoveryCodes.Contains(code))
        {
            user.RecoveryCodes.Remove(code);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<int> CountCodesAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.RecoveryCodes.Count);
    }

    #endregion

    #region IUserLoginStore

    public Task AddLoginAsync(IdentityUser user, UserLoginInfo login, CancellationToken cancellationToken)
    {
        var existing = user.Logins.Find(l =>
            l.LoginProvider == login.LoginProvider && l.ProviderKey == login.ProviderKey);

        if (existing == null)
        {
            user.Logins.Add(new IdentityUserLogin
            {
                LoginProvider = login.LoginProvider,
                ProviderKey = login.ProviderKey,
                ProviderDisplayName = login.ProviderDisplayName ?? string.Empty
            });
        }
        return Task.CompletedTask;
    }

    public Task RemoveLoginAsync(IdentityUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        var login = user.Logins.Find(l =>
            l.LoginProvider == loginProvider && l.ProviderKey == providerKey);

        if (login != null)
        {
            user.Logins.Remove(login);
        }
        return Task.CompletedTask;
    }

    public Task<IList<UserLoginInfo>> GetLoginsAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var logins = user.Logins
            .Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName))
            .ToList();
        return Task.FromResult<IList<UserLoginInfo>>(logins);
    }

    public async Task<IdentityUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        var query = await GetQueryable();
        var queryable = query.Queryable.Where(x =>
            x.Logins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey));
        var results = await GetResults(queryable, GetQueryDescription(), query.PartitionKey);
        return results.FirstOrDefault();
    }

    #endregion

    #region IQueryableUserStore

    public IQueryable<IdentityUser> Users
    {
        get
        {
            var query = GetQueryable().GetAwaiter().GetResult();
            return query.Queryable;
        }
    }

    #endregion
}
