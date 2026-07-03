using Benday.CosmosDb.SampleApp.WebUi;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Benday.CosmosDb.SampleApp.Tests.IntegrationTests;

public class NoteControllerFixture : TestClassBase
{
    public NoteControllerFixture(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Index_Get_ReturnsSuccess()
    {
        // arrange
        var factory = new WebApplicationFactory<MarkerClassForTesting>();

        var client = factory.CreateClient();

        // act
        var response = await client.GetAsync("/note", TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(response);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (response.IsSuccessStatusCode == false)
        {
            WriteLine($"Content: {content}");
            TestUtilities.CheckForDependencyError(content);
        }

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Index_Post_NewNote()
    {
        // arrange
        var factory = new WebApplicationFactory<MarkerClassForTesting>();

        var client = factory.CreateClient();

        var now = DateTime.Now.Ticks.ToString();

        var contentToPost = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                { "Id", "" },
                { "Subject", $"subject_{now}" },
                { "Text", $"text_{now}" }
            });

        // act
        var response = await client.PostAsync("note/edit/", contentToPost, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(response);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
