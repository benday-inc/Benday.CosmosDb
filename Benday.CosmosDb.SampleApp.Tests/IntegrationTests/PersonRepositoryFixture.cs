using Benday.Common.Testing;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Benday.CosmosDb.SampleApp.Api.Repositories;
using Benday.CosmosDb.SampleApp.WebUi;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CosmosDb.SampleApp.Tests.IntegrationTests;
public class PersonRepositoryFixture
{
    [Fact]
    public async Task CreatePerson_PopulatesBaseClassProperties()
    {
        // arrange
        var factory = new CustomWebApplicationFactory<MarkerClassForTesting>();

        var client = factory.CreateClient();

        var repository = factory.CreateInstance<IPersonRepository>();

        AssertThat.IsNotNull(repository);

        var person = new Person()
        {
            OwnerId = "TestOwner",
            FirstName = "TestFirstName",
            LastName = "TestLastName"
        };

        // act

        await repository.SaveAsync(person);

        // assert

        AssertThatString.IsNotNullOrWhiteSpace(person.Id, "Id");
        AssertThatString.IsNotNullOrWhiteSpace(person.Etag, "Etag");
        AssertThat.IsTrue(person.TimestampUnixStyle > 0, "TimestampUnixStyle");
        AssertThat.AreNotEqual(DateTime.MinValue, person.Timestamp, "Timestamp");
    }

    [Fact]
    public async Task CreatePerson_Multiple_PopulatesBaseClassProperties()
    {
        // arrange
        var factory = new CustomWebApplicationFactory<MarkerClassForTesting>();

        var client = factory.CreateClient();

        var repository = factory.CreateInstance<IPersonRepository>();

        AssertThat.IsNotNull(repository);

        var persons = new Person[500];
        for (int i = 0; i < 500; i++)
        {
            persons[i] = new Person()
            {
                OwnerId = "TestOwner",
                FirstName = $"TestFirstName{i}",
                LastName = $"TestLastName{i}"
            };
        }

        // act
        await repository.SaveAsync(persons);

        // assert

        foreach (var person in persons)
        {
            AssertThatString.IsNotNullOrWhiteSpace(person.Id, "Id");
            AssertThatString.IsNotNullOrWhiteSpace(person.Etag, "Etag");
        }
        AssertThat.IsTrue(persons.All(p => p.TimestampUnixStyle > 0), "TimestampUnixStyle");
        AssertThat.AreNotEqual(DateTime.MinValue, persons.Select(p => p.Timestamp).FirstOrDefault(), "Timestamp");
    }
}
