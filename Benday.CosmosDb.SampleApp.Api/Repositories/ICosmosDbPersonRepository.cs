using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using System;
using System.Linq;

namespace Benday.CosmosDb.SampleApp.Api.Repositories;
public interface IPersonRepository : IOwnedItemRepository<Person>
{
    Task<Person?> GetPersonByEmailAddress(string emailAddress);
}
