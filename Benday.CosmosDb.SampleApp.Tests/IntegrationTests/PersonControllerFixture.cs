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
        var response = await client.GetAsync("person/");

        // assert
        Assert.NotNull(response);

        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode == false)
        {
            TestUtilities.CheckForDependencyError(content);
        }

        Assert.True(response.IsSuccessStatusCode);
    }
}
