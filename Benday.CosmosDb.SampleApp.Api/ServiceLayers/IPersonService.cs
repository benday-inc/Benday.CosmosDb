using Benday.CosmosDb.SampleApp.Api.DomainModels;
using System;
using System.Linq;

namespace Benday.CosmosDb.SampleApp.Api.ServiceLayers;

public interface IPersonService : IOwnedItemServiceBase<Person>
{
    Task<Person?> GetPersonByEmailAddress(string emailAddress);
}
