using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Benday.CosmosDb.ServiceLayers;
using System;
using System.Linq;

namespace Benday.CosmosDb.SampleApp.Api.ServiceLayers;

public interface IPersonService : IOwnedItemServiceBase<Person>
{
    Task<Person?> GetPersonByEmailAddress(string emailAddress);
}
