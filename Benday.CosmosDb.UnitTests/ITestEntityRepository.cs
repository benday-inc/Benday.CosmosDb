using Benday.CosmosDb.Repositories;

namespace Benday.CosmosDb.UnitTests;

public interface ITestEntityRepository : ITenantItemRepository<TestEntity>
{

}