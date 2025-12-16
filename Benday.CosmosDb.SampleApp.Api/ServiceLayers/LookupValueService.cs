using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Benday.CosmosDb.SampleApp.Api.Repositories;
using Benday.CosmosDb.ServiceLayers;

namespace Benday.CosmosDb.SampleApp.Api.ServiceLayers;

public class LookupValueService : OwnedItemService<LookupValue>, ILookupValueService
{
    public LookupValueService(ILookupValueRepository repository) : base(repository)
    {
    }

    public override Task<LookupValue?> SaveAsync(LookupValue item)
    {
        if (string.IsNullOrEmpty(item.OwnerId) == true)
        {
            item.OwnerId = ApiConstants.DEFAULT_OWNER_ID;
        }

        return base.SaveAsync(item);
    }
}
