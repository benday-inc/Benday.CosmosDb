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
public class CosmosDbPersonRepository : CosmosOwnedItemRepository<Person>, IPersonRepository
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
        var queryable = await GetQueryable(ApiConstants.DEFAULT_OWNER_ID);

        var query = queryable.Queryable.Where(x => x.EmailAddress == emailAddress);

        var results = await GetResults(
            query,
            GetQueryDescription(nameof(GetPersonByEmailAddress)), 
            queryable.PartitionKey);

        return results.FirstOrDefault();
    }
}
