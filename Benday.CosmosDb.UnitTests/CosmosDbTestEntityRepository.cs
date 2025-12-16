using Benday.CosmosDb.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.CosmosDb.UnitTests;

public class CosmosDbTestEntityRepository : CosmosOwnedItemRepository<TestEntity>, ITestEntityRepository
{
    public CosmosDbTestEntityRepository(
        IOptions<CosmosRepositoryOptions<TestEntity>> options,
        CosmosClient client,
        ILogger<CosmosDbTestEntityRepository> logger) :
        base(options, client, logger)
    {
    }

    
}
