using System.Reflection;
using Benday.CosmosDb.Utilities;
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