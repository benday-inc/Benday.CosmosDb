using System.Reflection;
using Benday.CosmosDb.Utilities;
using Benday.Common;
using Benday.Common.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Benday.CosmosDb.UnitTests;


public class CosmosRegistrationHelperFixture : Benday.Common.Testing.TestClassBase
{
    public CosmosRegistrationHelperFixture(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        
    }

    [Fact]
    public void VerifyConnectionRegistration_FromConfig_UseDefaultAzureCredentials()
    {
        // Create an in-memory IConfiguration
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "CosmosConfiguration:AccountKey", "fakeAccountKey" },
            { "CosmosConfiguration:Endpoint", "https://example.com" },
            { "CosmosConfiguration:DatabaseName", "TestDatabase" },
            { "CosmosConfiguration:ContainerName", "TestContainer" },
            { "CosmosConfiguration:PartitionKey", "/TestPartitionKey" },
            { "CosmosConfiguration:WithCreateStructures", "true" },
            { "CosmosConfiguration:Throughput", "400" },
            { "CosmosConfiguration:UseGatewayMode", "false" },
            { "CosmosConfiguration:UseHierarchicalPartitionKey", "false" },
            { "CosmosConfiguration:AllowBulkExecution", "true" },
            { "CosmosConfiguration:UseDefaultAzureCredential", "true" }
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var config = configuration.GetCosmosConfig();

        
        Assert.NotNull(config);
        Assert.Equal(string.Empty, config.AccountKey);
        Assert.Equal("https://example.com", config.Endpoint);
        Assert.Equal("TestDatabase", config.DatabaseName);
        Assert.Equal("TestContainer", config.ContainerName);
        Assert.Equal("/TestPartitionKey", config.PartitionKey);        
        Assert.False(config.UseGatewayMode);
        Assert.False(config.UseHierarchicalPartitionKey);
        Assert.True(config.AllowBulkExecution);
        Assert.True(config.UseDefaultAzureCredential);
    }

    [Fact]
    public void VerifyConnectionRegistration_FromConfig_ConnectionString()
    {
        // Create an in-memory IConfiguration
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "CosmosConfiguration:AccountKey", "fakeAccountKey" },
            { "CosmosConfiguration:Endpoint", "https://example.com" },
            { "CosmosConfiguration:DatabaseName", "TestDatabase" },
            { "CosmosConfiguration:ContainerName", "TestContainer" },
            { "CosmosConfiguration:PartitionKey", "/TestPartitionKey" },
            { "CosmosConfiguration:WithCreateStructures", "true" },
            { "CosmosConfiguration:Throughput", "400" },
            { "CosmosConfiguration:UseGatewayMode", "false" },
            { "CosmosConfiguration:UseHierarchicalPartitionKey", "false" },
            { "CosmosConfiguration:AllowBulkExecution", "true" },
            { "CosmosConfiguration:UseDefaultAzureCredential", "false" }
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var config = configuration.GetCosmosConfig();

         Assert.NotNull(config);
        Assert.Equal("fakeAccountKey", config.AccountKey);
        Assert.Equal("https://example.com", config.Endpoint);
        Assert.Equal("TestDatabase", config.DatabaseName);
        Assert.Equal("TestContainer", config.ContainerName);
        Assert.Equal("/TestPartitionKey", config.PartitionKey);        
        Assert.False(config.UseGatewayMode);
        Assert.False(config.UseHierarchicalPartitionKey);
        Assert.True(config.AllowBulkExecution);
        Assert.False(config.UseDefaultAzureCredential);
    }

    [Fact]
    public void VerifyConnectionRegistration_UseDefaultAzureCredentials()
    {
        var servicesMock = new Moq.Mock<IServiceCollection>();

        var descriptors = new List<ServiceDescriptor>();

        servicesMock.Setup(x => x.Add(It.IsAny<ServiceDescriptor>()))
            .Callback<ServiceDescriptor>(sd =>
            {
                descriptors.Add(sd);
            });


        var config = new CosmosConfig(
            "accountKey",
            "https://example.com",
            "TestDatabase",
            "TestContainer",
            "/TestPartitionKey",
            true,
            400,
            false,
            false,
            true,
            true);

        var systemUnderTest =
             new CosmosRegistrationHelper(servicesMock.Object,
                 config);

        Assert.NotNull(systemUnderTest);
        Assert.True(descriptors.Count == 1);

        var descriptor = descriptors[0];
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(CosmosClient), descriptor.ServiceType);
        Assert.IsType<CosmosClient>(descriptor.ImplementationInstance);
        var instance = (CosmosClient)descriptor.ImplementationInstance;
        var options = instance.ClientOptions;

        Assert.Equal("https://example.com/", instance.Endpoint.AbsoluteUri);
        Assert.Equal("TestDatabase", systemUnderTest.DatabaseName);
        Assert.Equal("TestContainer", systemUnderTest.ContainerName);
        Assert.Equal("/TestPartitionKey", systemUnderTest.PartitionKey);
        Assert.True(systemUnderTest.WithCreateStructures);
        Assert.False(systemUnderTest.UseGatewayMode);
        Assert.False(systemUnderTest.UseHierarchicalPartitionKey);
        Assert.True(systemUnderTest.AllowBulkExecution);
        Assert.True(systemUnderTest.UseDefaultAzureCredential);

        
        CheckInstance(instance, "AuthorizationTokenProviderTokenCredential");
        
        WriteLine($"message");
    }

[Fact]
    public void VerifyConnectionRegistration_UseConnectionString()
    {
        var servicesMock = new Moq.Mock<IServiceCollection>();

        var descriptors = new List<ServiceDescriptor>();

        servicesMock.Setup(x => x.Add(It.IsAny<ServiceDescriptor>()))
            .Callback<ServiceDescriptor>(sd =>
            {
                descriptors.Add(sd);
            });


        // create a fake base64 encoded account key
        var accountKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("fakeAccountKey"));

        var config = new CosmosConfig(
            accountKey,
            "https://example.com",
            "TestDatabase",
            "TestContainer",
            "/TestPartitionKey",
            true,
            400,
            false,
            false,
            true,
            false);

        var systemUnderTest =
             new CosmosRegistrationHelper(servicesMock.Object,
                 config);

        Assert.NotNull(systemUnderTest);
        Assert.True(descriptors.Count == 1);

        var descriptor = descriptors[0];
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(CosmosClient), descriptor.ServiceType);
        Assert.IsType<CosmosClient>(descriptor.ImplementationInstance);
        var instance = (CosmosClient)descriptor.ImplementationInstance;
        var options = instance.ClientOptions;

        Assert.Equal("https://example.com/", instance.Endpoint.AbsoluteUri);
        Assert.Equal("TestDatabase", systemUnderTest.DatabaseName);
        Assert.Equal("TestContainer", systemUnderTest.ContainerName);
        Assert.Equal("/TestPartitionKey", systemUnderTest.PartitionKey);
        Assert.True(systemUnderTest.WithCreateStructures);
        Assert.False(systemUnderTest.UseGatewayMode);
        Assert.False(systemUnderTest.UseHierarchicalPartitionKey);
        Assert.True(systemUnderTest.AllowBulkExecution);
        Assert.False(systemUnderTest.UseDefaultAzureCredential);

        CheckInstance(instance, "AuthorizationTokenProviderMasterKey");
        
        WriteLine($"message");
    }


    private void CheckInstance(CosmosClient instance, string expectedTokenProviderTypeName)
    {
        // using reflection, get this field internal AuthorizationTokenProvider AuthorizationTokenProvider { get; }
        var field = typeof(CosmosClient).GetProperty("AuthorizationTokenProvider",
                BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var tokenProvider = field.GetValue(instance);
        Assert.NotNull(tokenProvider);
        WriteLine($"message: {tokenProvider.GetType().FullName}");

        var fullName = tokenProvider.GetType().FullName ?? string.Empty;

        Assert.True(fullName.Contains(expectedTokenProviderTypeName),
            $"Expected token provider type name to contain '{expectedTokenProviderTypeName}', but was '{fullName}'");
    }
}