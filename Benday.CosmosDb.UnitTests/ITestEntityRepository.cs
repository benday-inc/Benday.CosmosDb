using Benday.CosmosDb.Repositories;

namespace Benday.CosmosDb.UnitTests;

public interface ITestEntityRepository : IOwnedItemRepository<TestEntity>
{

}