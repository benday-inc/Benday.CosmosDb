using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CosmosDb.SampleApp.Api.Repositories;
public class CosmosDbPersonRepository : CosmosTenantItemRepository<Person>, IPersonRepository
{
    public CosmosDbPersonRepository(
        IOptions<CosmosRepositoryOptions<Person>> options, 
        CosmosClient client, 
        ILogger<CosmosDbPersonRepository> logger) :
        base(options, client, logger)
    {

    }

    public async Task<Person?> GetPersonByEmailAddress(string emailAddress)
    {
        var queryContext = await GetQueryContextAsync(ApiConstants.DEFAULT_TENANT_ID);

        var query = queryContext.Queryable.Where(x => x.EmailAddress == emailAddress);

        var results = await GetResultsAsync(
            query,
            GetQueryDescription(nameof(GetPersonByEmailAddress)),
            queryContext.PartitionKey);

        return results.FirstOrDefault();
    }
}
