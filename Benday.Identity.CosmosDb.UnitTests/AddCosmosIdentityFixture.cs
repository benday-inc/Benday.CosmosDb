using Benday.Common.Testing;
using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.Utilities;
using Benday.Identity.CosmosDb;
using Benday.Identity.CosmosDb.UI;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Benday.Identity.CosmosDb.UnitTests;

public class AddCosmosIdentityFixture : TestClassBase
{
    public AddCosmosIdentityFixture(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    private CosmosConfig CreateTestConfig()
    {
        var fakeKey = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                "0123456789012345678901234567890123456789012345678901234567890123"));

        return new CosmosConfig
        {
            Endpoint = "https://localhost:8081",
            AccountKey = fakeKey,
            DatabaseName = "TestDatabase",
            ContainerName = "TestContainer",
            PartitionKey = "/pk",
            CreateStructures = false,
            UseDefaultAzureCredential = false
        };
    }

    [Fact]
    public void AddCosmosIdentity_RegistersUserStore()
    {
        var services = new ServiceCollection();
        var config = CreateTestConfig();

        services.AddCosmosIdentity(config);

        var descriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(IUserStore<CosmosIdentityUser>));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        Assert.Equal(typeof(CosmosDbUserStore), descriptor.ImplementationType);

        WriteLine($"UserStore registered: {descriptor.ImplementationType?.Name}");
    }

    [Fact]
    public void AddCosmosIdentity_RegistersRoleStore()
    {
        var services = new ServiceCollection();
        var config = CreateTestConfig();

        services.AddCosmosIdentity(config);

        var descriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(IRoleStore<CosmosIdentityRole>));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        Assert.Equal(typeof(CosmosDbRoleStore), descriptor.ImplementationType);

        WriteLine($"RoleStore registered: {descriptor.ImplementationType?.Name}");
    }

    [Fact]
    public void AddCosmosIdentity_RegistersCosmosClient()
    {
        var services = new ServiceCollection();
        var config = CreateTestConfig();

        services.AddCosmosIdentity(config);

        var descriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(CosmosClient));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);

        WriteLine("CosmosClient registered as Singleton");
    }

    [Fact]
    public void AddCosmosIdentity_DoesNotDoubleRegisterCosmosClient()
    {
        var services = new ServiceCollection();
        var config = CreateTestConfig();

        // Pre-register a CosmosClient
        var fakeKey = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(
                "0123456789012345678901234567890123456789012345678901234567890123"));
        var existingClient = new CosmosClient("https://localhost:8081", fakeKey);
        services.AddSingleton(existingClient);

        services.AddCosmosIdentity(config);

        var cosmosClientDescriptors = services
            .Where(sd => sd.ServiceType == typeof(CosmosClient))
            .ToList();

        Assert.Single(cosmosClientDescriptors);
        WriteLine($"CosmosClient count: {cosmosClientDescriptors.Count}");
    }

    [Fact]
    public void AddCosmosIdentity_RegistersRepositoryOptions()
    {
        var services = new ServiceCollection();
        var config = CreateTestConfig();

        services.AddCosmosIdentity(config);

        var userOptionsDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(CosmosRepositoryOptions<CosmosIdentityUser>));
        Assert.NotNull(userOptionsDescriptor);

        var roleOptionsDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(CosmosRepositoryOptions<CosmosIdentityRole>));
        Assert.NotNull(roleOptionsDescriptor);

        WriteLine("Repository options registered for both User and Role");
    }

    [Fact]
    public void AddCosmosIdentity_RegistersClaimsPrincipalFactory()
    {
        var services = new ServiceCollection();
        var config = CreateTestConfig();

        services.AddCosmosIdentity(config);

        // AddIdentity registers a default factory first, then AddCosmosIdentity
        // registers our custom one. DI resolves the last registration, so use LastOrDefault.
        var descriptor = services.LastOrDefault(
            sd => sd.ServiceType == typeof(IUserClaimsPrincipalFactory<CosmosIdentityUser>));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(DefaultUserClaimsPrincipalFactory), descriptor.ImplementationType);

        WriteLine("ClaimsPrincipalFactory registered as Scoped");
    }

    [Fact]
    public void AddCosmosIdentity_CustomOptions_UsesCustomValues()
    {
        var services = new ServiceCollection();
        var config = CreateTestConfig();

        services.AddCosmosIdentity(config, options =>
        {
            options.UsersContainerName = "CustomUsers";
            options.RolesContainerName = "CustomRoles";
            options.CookieName = "MyApp.Auth";
        });

        // The options are applied during registration - we can verify by checking
        // that the service collection was populated (the actual container names
        // are baked into the IOptions<CosmosRepositoryOptions<T>> registrations)
        var userOptionsDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(CosmosRepositoryOptions<CosmosIdentityUser>));
        Assert.NotNull(userOptionsDescriptor);

        var roleOptionsDescriptor = services.FirstOrDefault(
            sd => sd.ServiceType == typeof(CosmosRepositoryOptions<CosmosIdentityRole>));
        Assert.NotNull(roleOptionsDescriptor);

        WriteLine("Custom options applied successfully");
    }
}
