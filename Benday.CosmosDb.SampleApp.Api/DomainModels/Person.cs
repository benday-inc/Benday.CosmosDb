using System.ComponentModel.DataAnnotations;
using Benday.CosmosDb.DomainModels;

namespace Benday.CosmosDb.SampleApp.Api.DomainModels;

public class Person : OwnedItemBase
{
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;
    
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;
    
    [Display(Name = "Email Address")]
    public string EmailAddress { get; set; } = string.Empty;

    public List<Address> Addresses { get; set; } = new();
}
