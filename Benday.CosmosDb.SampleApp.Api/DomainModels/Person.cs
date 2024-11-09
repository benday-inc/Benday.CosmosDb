using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.SampleApp.Api.DomainModels;

public class Person : OwnedItemBase
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;

    public List<Address> Addresses { get; set; } = new();
}
