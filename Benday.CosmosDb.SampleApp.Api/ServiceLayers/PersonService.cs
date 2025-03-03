using Benday.CosmosDb.DomainModels;
using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Benday.CosmosDb.SampleApp.Api.Repositories;
using Benday.CosmosDb.ServiceLayers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CosmosDb.SampleApp.Api.ServiceLayers;

public class PersonService : OwnedItemService<Person>, IPersonService
{
    private IPersonRepository _Repository;

    public PersonService(IPersonRepository repository) : base(repository)
    {
        _Repository = repository;
    }

    public async Task<Person?> GetPersonByEmailAddress(string emailAddress)
    {
        return await _Repository.GetPersonByEmailAddress(emailAddress);
    }

    public override Task<Person?> SaveAsync(Person item)
    {
        if (string.IsNullOrEmpty(item.OwnerId) == true)
        {
            item.OwnerId = ApiConstants.DEFAULT_OWNER_ID;
        }
        
        return base.SaveAsync(item);
    }
}