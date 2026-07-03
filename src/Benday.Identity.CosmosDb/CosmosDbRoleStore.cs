using System.Security.Claims;
using Benday.CosmosDb.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.Identity.CosmosDb
{
    /// <summary>
    /// ASP.NET Core Identity role store backed by Cosmos DB. Provides role
    /// persistence, role claim management, and queryable role access.
    /// </summary>
    public class CosmosDbRoleStore :
        CosmosTenantItemRepository<CosmosIdentityRole>,
        IRoleStore<CosmosIdentityRole>,
        IRoleClaimStore<CosmosIdentityRole>,
        IQueryableRoleStore<CosmosIdentityRole>
    {
        private readonly string _identityTenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbRoleStore"/> class.
        /// </summary>
        /// <param name="options">Repository options for the identity role container.</param>
        /// <param name="client">The Cosmos DB client used to access the container.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        /// <param name="identityOptions">Identity options that supply the tenant id used for role storage.</param>
        public CosmosDbRoleStore(
           IOptions<CosmosRepositoryOptions<CosmosIdentityRole>> options,
           CosmosClient client, ILogger<CosmosDbRoleStore> logger,
           CosmosIdentityOptions identityOptions) :
           base(options, client, logger)
        {
            _identityTenantId = identityOptions.IdentityTenantId;
        }

        #region IRoleStore

        /// <inheritdoc/>
        public async Task<IdentityResult> CreateAsync(CosmosIdentityRole role, CancellationToken cancellationToken)
        {
            await SaveAsync(role);
            return IdentityResult.Success;
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> DeleteAsync(CosmosIdentityRole role, CancellationToken cancellationToken)
        {
            await DeleteAsync(role);
            return IdentityResult.Success;
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> UpdateAsync(CosmosIdentityRole role, CancellationToken cancellationToken)
        {
            role.ConcurrencyStamp = Guid.NewGuid().ToString();
            await SaveAsync(role);
            return IdentityResult.Success;
        }

        /// <inheritdoc/>
        public async Task<CosmosIdentityRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            return await GetByIdAsync(_identityTenantId, roleId);
        }

        /// <inheritdoc/>
        public async Task<CosmosIdentityRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            var queryContext = await GetQueryContextAsync(_identityTenantId);
            var queryable = queryContext.Queryable.Where(x => x.NormalizedName == normalizedRoleName);
            var results = await GetResultsAsync(queryable, GetQueryDescription(), queryContext.PartitionKey);
            return results.FirstOrDefault();
        }

        /// <inheritdoc/>
        public Task<string> GetRoleIdAsync(CosmosIdentityRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Id);
        }

        /// <inheritdoc/>
        public Task<string?> GetRoleNameAsync(CosmosIdentityRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(role.Name);
        }

        /// <inheritdoc/>
        public Task SetRoleNameAsync(CosmosIdentityRole role, string? roleName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                throw new ArgumentException($"{nameof(roleName)} is null or empty.", nameof(roleName));
            }

            role.Name = roleName;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<string?> GetNormalizedRoleNameAsync(CosmosIdentityRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult<string?>(role.NormalizedName);
        }

        /// <inheritdoc/>
        public Task SetNormalizedRoleNameAsync(CosmosIdentityRole role, string? normalizedName, CancellationToken cancellationToken)
        {
            // no-op: normalized value is computed from Name
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        #endregion

        #region IRoleClaimStore

        /// <inheritdoc/>
        public Task AddClaimAsync(CosmosIdentityRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            // Check for duplicates before adding
            var existing = role.Claims.Find(c => c.Type == claim.Type && c.Value == claim.Value);
            if (existing == null)
            {
                var roleClaim = new CosmosIdentityClaim
                {
                    Type = claim.Type,
                    Value = claim.Value
                };
                role.Claims.Add(roleClaim);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IList<Claim>> GetClaimsAsync(CosmosIdentityRole role, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IList<Claim>>(role.Claims.ToClaimList());
        }

        /// <inheritdoc/>
        public Task RemoveClaimAsync(CosmosIdentityRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            var roleClaim = role.Claims.Find(x => x.Type == claim.Type && x.Value == claim.Value);

            if (roleClaim != null)
            {
                role.Claims.Remove(roleClaim);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region IQueryableRoleStore

        /// <inheritdoc/>
        public IQueryable<CosmosIdentityRole> Roles
        {
            get
            {
                var queryContext = GetQueryContextAsync(_identityTenantId).GetAwaiter().GetResult();
                return queryContext.Queryable;
            }
        }

        #endregion
    }
}
