using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Benday.CosmosDb.ServiceLayers;

namespace Benday.CosmosDb.SampleApp.Api.ServiceLayers;

public interface ILookupValueService : IOwnedItemService<LookupValue>
{
}