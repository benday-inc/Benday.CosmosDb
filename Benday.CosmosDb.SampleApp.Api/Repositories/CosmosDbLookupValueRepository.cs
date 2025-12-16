using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.CosmosDb.SampleApp.Api.Repositories;

public class CosmosDbLookupValueRepository : CosmosOwnedItemRepository<LookupValue>, ILookupValueRepository
{
    public CosmosDbLookupValueRepository(
        IOptions<CosmosRepositoryOptions<LookupValue>> options,
        CosmosClient client,
        ILogger<CosmosDbLookupValueRepository> logger) :
        base(options, client, logger)
    {
    }

    
}
