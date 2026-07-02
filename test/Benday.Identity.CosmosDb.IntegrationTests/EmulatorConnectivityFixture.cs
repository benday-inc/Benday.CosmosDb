using Benday.Common.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.IntegrationTests;

[Collection("CosmosEmulator")]
public class EmulatorConnectivityFixture : IntegrationTestBase
{
    public EmulatorConnectivityFixture(
        CosmosEmulatorFixture emulator,
        ITestOutputHelper outputHelper) : base(emulator, outputHelper)
    {
    }

    [Fact]
    public void EmulatorFixture_ClientIsNotNull()
    {
        Assert.NotNull(Emulator.Client);
        WriteLine("CosmosClient instance is available");
    }

    [Fact]
    public void EmulatorFixture_ConfigIsNotNull()
    {
        Assert.NotNull(Emulator.Config);
        WriteLine($"Config endpoint: {Emulator.Config.Endpoint}");
        WriteLine($"Config database: {Emulator.Config.DatabaseName}");
    }

    [Fact]
    public async Task CanReadDatabaseAccount()
    {
        var account = await Emulator.Client.ReadAccountAsync();

        Assert.NotNull(account);
        WriteLine($"Connected to Cosmos DB account: {account.Id}");
    }

    [Fact]
    public async Task CanAccessDatabase()
    {
        var database = Emulator.Client.GetDatabase(CosmosEmulatorFixture.DatabaseName);
        var response = await database.ReadAsync();

        Assert.NotNull(response);
        Assert.Equal(CosmosEmulatorFixture.DatabaseName, response.Database.Id);
        WriteLine($"Database '{response.Database.Id}' exists");
    }

    [Fact]
    public async Task CanAccessUsersContainer()
    {
        var database = Emulator.Client.GetDatabase(CosmosEmulatorFixture.DatabaseName);
        var container = database.GetContainer(CosmosEmulatorFixture.UsersContainerName);
        var response = await container.ReadContainerAsync();

        Assert.NotNull(response);
        Assert.Equal(CosmosEmulatorFixture.UsersContainerName, response.Resource.Id);
        WriteLine($"Container '{response.Resource.Id}' exists");
    }

    [Fact]
    public async Task CanAccessRolesContainer()
    {
        var database = Emulator.Client.GetDatabase(CosmosEmulatorFixture.DatabaseName);
        var container = database.GetContainer(CosmosEmulatorFixture.RolesContainerName);
        var response = await container.ReadContainerAsync();

        Assert.NotNull(response);
        Assert.Equal(CosmosEmulatorFixture.RolesContainerName, response.Resource.Id);
        WriteLine($"Container '{response.Resource.Id}' exists");
    }
}
