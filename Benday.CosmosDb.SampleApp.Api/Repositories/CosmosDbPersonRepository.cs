using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Microsoft.Azure.Cosmos;
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
        IOptions<CosmosRepositoryOptions<Person>> options, CosmosClient client) :
        base(options, client)
    {

    }

    public async Task<Person?> GetPersonByEmailAddress(string emailAddress)
    {
        var container = await GetContainer();

        var queryable = container.GetItemLinqQueryable<Person>();

        var query = queryable.Where(x => x.EmailAddress == emailAddress);

        var results = await GetResults(query,
            GetQueryDescription(nameof(GetPersonByEmailAddress)));

        return results.FirstOrDefault();
    }
}
