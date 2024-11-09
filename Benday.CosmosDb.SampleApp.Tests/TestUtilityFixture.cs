using Xunit.Abstractions;

namespace Benday.CosmosDb.SampleApp.Tests;

public class TestUtilityFixture
{
    private readonly ITestOutputHelper _Output;

    public TestUtilityFixture(ITestOutputHelper output)
    {
        _Output = output;
    }

    private void WriteLine(string message)
    {
        _Output.WriteLine(message);
    }

    [Fact]
    public void ParseTestDependencyReturnsExpectedErrorMessage()
    {
        // arrange
        var input = "Unable to resolve service for type 'Benday.DemoApp.Api.IDecisionService' while attempting to activate 'Benday.DemoApp.Web.Controllers.DecisionController' blah blah blah";

        var expected = "Dependency error. Interface 'Benday.DemoApp.Api.IDecisionService' is not resolved to concrete type 'Benday.DemoApp.Web.Controllers.DecisionController'.";

        // act
        var actual = TestUtilities.ParseDependencyError(input);

        // assert

        WriteLine($"Expected: {expected}");
        WriteLine($"Actual  : {actual}");

        Assert.Equal(expected, actual);
    }
}