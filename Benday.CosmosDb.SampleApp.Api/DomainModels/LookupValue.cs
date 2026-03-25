using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.SampleApp.Api.DomainModels;

public class LookupValue : TenantItemBase
{
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}