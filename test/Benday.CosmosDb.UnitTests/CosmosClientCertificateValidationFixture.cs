using Benday.CosmosDb.Utilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benday.CosmosDb.UnitTests;

/// <summary>
/// TLS server certificate validation must stay on unless the client targets the
/// emulator (loopback endpoint) or the caller explicitly turns it off. The bypass
/// is implemented by installing a custom HttpClientFactory.
/// </summary>
public class CosmosClientCertificateValidationFixture
{
    private const string FakeAccountKey =
        "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    private static CosmosConfig BuildConfig(string endpoint, bool disableValidation = false) => new()
    {
        AccountKey = FakeAccountKey,
        Endpoint = endpoint,
        DatabaseName = "TestDatabase",
        ContainerName = "TestContainer",
        PartitionKey = "/tenantId,/entityType",
        UseGatewayMode = true,
        DisableServerCertificateValidation = disableValidation
    };

    private static CosmosClient GetRegisteredClient(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<CosmosClient>();
    }

    [Fact]
    public void GetCosmosDbClientOptions_Default_ValidatesServerCertificate()
    {
        var options = CosmosClientOptionsUtilities.GetCosmosDbClientOptions(null);

        Assert.Null(options.HttpClientFactory);
    }

    [Fact]
    public void GetCosmosDbClientOptions_DisableValidation_InstallsHttpClientFactory()
    {
        var options = CosmosClientOptionsUtilities.GetCosmosDbClientOptions(
            null, ConnectionMode.Gateway, false, disableServerCertificateValidation: true);

        Assert.NotNull(options.HttpClientFactory);
    }

    [Fact]
    public void ConfigureCosmosClient_RemoteEndpoint_ValidatesServerCertificate()
    {
        var services = new ServiceCollection();

        services.ConfigureCosmosClient(BuildConfig("https://example.documents.azure.com:443/"));

        Assert.Null(GetRegisteredClient(services).ClientOptions.HttpClientFactory);
    }

    [Theory]
    [InlineData("https://localhost:8081/")]
    [InlineData("https://127.0.0.1:8081/")]
    public void ConfigureCosmosClient_LoopbackEndpoint_SkipsValidation(string endpoint)
    {
        var services = new ServiceCollection();

        services.ConfigureCosmosClient(BuildConfig(endpoint));

        Assert.NotNull(GetRegisteredClient(services).ClientOptions.HttpClientFactory);
    }

    [Fact]
    public void ConfigureCosmosClient_RemoteEndpointWithFlag_SkipsValidation()
    {
        var services = new ServiceCollection();

        services.ConfigureCosmosClient(BuildConfig("https://cosmos-emulator:8081/", disableValidation: true));

        Assert.NotNull(GetRegisteredClient(services).ClientOptions.HttpClientFactory);
    }

    [Fact]
    public void ConfigureCosmosClient_ConnectionString_RemoteEndpoint_ValidatesServerCertificate()
    {
        var services = new ServiceCollection();

        services.ConfigureCosmosClient(
            $"AccountEndpoint=https://example.documents.azure.com:443/;AccountKey={FakeAccountKey};",
            useGatewayMode: true);

        Assert.Null(GetRegisteredClient(services).ClientOptions.HttpClientFactory);
    }

    [Fact]
    public void ConfigureCosmosClient_ConnectionString_LoopbackEndpoint_SkipsValidation()
    {
        var services = new ServiceCollection();

        services.ConfigureCosmosClient(
            $"AccountEndpoint=https://localhost:8081/;AccountKey={FakeAccountKey};",
            useGatewayMode: true);

        Assert.NotNull(GetRegisteredClient(services).ClientOptions.HttpClientFactory);
    }

    [Fact]
    public void ForEmulator_UsesLoopbackEndpoint()
    {
        var config = new CosmosConfigBuilder()
            .ForEmulator()
            .WithDatabase("TestDatabase")
            .WithContainer("TestContainer")
            .WithPartitionKey("/tenantId,/entityType", useHierarchical: true)
            .Build();

        var services = new ServiceCollection();

        services.ConfigureCosmosClient(config);

        Assert.NotNull(GetRegisteredClient(services).ClientOptions.HttpClientFactory);
    }
}
