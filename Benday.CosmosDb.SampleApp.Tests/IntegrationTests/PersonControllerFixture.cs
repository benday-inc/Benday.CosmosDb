using Benday.CosmosDb.SampleApp.WebUi;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benday.CosmosDb.SampleApp.Tests.IntegrationTests;
public class PersonControllerFixture
{
    [Fact]
    public async Task Index_Get_ReturnsSuccess()
    {
        // arrange
        var factory = new WebApplicationFactory<MarkerClassForTesting>();

        var client = factory.CreateClient();

        // act
        var response = await client.GetAsync("/person");

        // assert
        Assert.NotNull(response);

        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode == false)
        {
            TestUtilities.CheckForDependencyError(content);
        }

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Index_Post_NewPerson()
    {
        // arrange
        var factory = new WebApplicationFactory<MarkerClassForTesting>();

        var client = factory.CreateClient();

        var now = DateTime.Now.Ticks.ToString();

        var contentToPost = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "Id", "" },
                { "FirstName", $"fn_{now}" },
                { "LastName", $"ln_{now}" },
                { "EmailAddress", $"email_{now}" }
            });

        // act
        var response = await client.PostAsync("person/edit/", contentToPost);

        // assert
        Assert.NotNull(response);

        var content = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
    }
}
