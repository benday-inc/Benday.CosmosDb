using System;
using System.Text;
using Benday.CosmosDb.Diagnostics;
using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.Utilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Benday.CosmosDb.UnitTests.Diagnostics;

public class CosmosDiagnosticsRegistryFixture
{
    private sealed class OtherTestEntity : TestEntity { }

    private static CosmosConfig BuildConfig() => new()
    {
        AccountKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("k")),
        Endpoint = "https://example.com",
        DatabaseName = "Db",
        ContainerName = "C",
        PartitionKey = "/tenantId,/entityType",
        UseHierarchicalPartitionKey = true,
        AllowBulkExecution = true,
        UseDefaultAzureCredential = false
    };

    [Fact]
    public void GetFor_NoEntryConfigured_ReturnsDefault()
    {
        var registry = new CosmosDiagnosticsRegistry();

        var options = registry.GetFor<TestEntity>();

        Assert.NotNull(options);
        Assert.False(options.CaptureIndexMetrics);
    }

    [Fact]
    public void Set_MutatesEntityOptions_OtherEntitiesUseDefault()
    {
        var registry = new CosmosDiagnosticsRegistry();

        registry.Set<TestEntity>(o => o.CaptureIndexMetrics = true);

        Assert.True(registry.GetFor<TestEntity>().CaptureIndexMetrics);
        Assert.False(registry.GetFor<OtherTestEntity>().CaptureIndexMetrics);
    }

    [Fact]
    public void SetDefault_AppliesToUnconfiguredEntities_PerEntityWins()
    {
        var registry = new CosmosDiagnosticsRegistry();

        registry.SetDefault(o => o.CaptureIndexMetrics = true);
        registry.Set<TestEntity>(o => o.CaptureIndexMetrics = false);

        Assert.False(registry.GetFor<TestEntity>().CaptureIndexMetrics);
        Assert.True(registry.GetFor<OtherTestEntity>().CaptureIndexMetrics);
    }

    [Fact]
    public void Set_NullCallback_Throws()
    {
        var registry = new CosmosDiagnosticsRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Set<TestEntity>(null!));
    }

    [Fact]
    public void SetDefault_NullCallback_Throws()
    {
        var registry = new CosmosDiagnosticsRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.SetDefault(null!));
    }

    [Fact]
    public void Di_Defaults_ResolveRegistryWithAllDefaultsOff()
    {
        var services = new ServiceCollection();
        _ = new CosmosRegistrationHelper(services, BuildConfig());

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<CosmosDiagnosticsRegistry>();

        Assert.False(registry.GetFor<TestEntity>().CaptureIndexMetrics);
    }

    [Fact]
    public void Di_ConfigureDiagnostics_MutatesSameInstanceResolvedFromDi()
    {
        var services = new ServiceCollection();
        var helper = new CosmosRegistrationHelper(services, BuildConfig());

        helper.ConfigureDiagnostics<TestEntity>(o => o.CaptureIndexMetrics = true);

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<CosmosDiagnosticsRegistry>();

        Assert.True(registry.GetFor<TestEntity>().CaptureIndexMetrics);
        Assert.False(registry.GetFor<OtherTestEntity>().CaptureIndexMetrics);
    }

    [Fact]
    public void Di_ConfigureDiagnosticsDefault_AppliesAcrossEntityTypes()
    {
        var services = new ServiceCollection();
        var helper = new CosmosRegistrationHelper(services, BuildConfig());

        helper.ConfigureDiagnosticsDefault(o => o.CaptureIndexMetrics = true);

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<CosmosDiagnosticsRegistry>();

        Assert.True(registry.GetFor<TestEntity>().CaptureIndexMetrics);
        Assert.True(registry.GetFor<OtherTestEntity>().CaptureIndexMetrics);
    }

    [Fact]
    public void Di_ConfigureDiagnostics_BeforeAndAfterRegistration_TargetsSameRegistry()
    {
        // The order should not matter — both calls must end up on the same
        // singleton instance that repositories will resolve.
        var services = new ServiceCollection();
        var helper = new CosmosRegistrationHelper(services, BuildConfig());

        helper.ConfigureDiagnostics<TestEntity>(o => o.CaptureIndexMetrics = true);
        helper.RegisterRepositoryAndService<TestEntity>();
        helper.ConfigureDiagnostics<OtherTestEntity>(o => o.CaptureIndexMetrics = true);

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<CosmosDiagnosticsRegistry>();

        Assert.True(registry.GetFor<TestEntity>().CaptureIndexMetrics);
        Assert.True(registry.GetFor<OtherTestEntity>().CaptureIndexMetrics);
    }

    [Fact]
    public void Di_ConfigureDiagnostics_BeforeHelperConstructed_PreservedOnHelperInit()
    {
        // Pre-existing registry registration on the service collection must
        // not be replaced by the helper's TryAddSingleton default.
        var services = new ServiceCollection();
        var preRegistered = new CosmosDiagnosticsRegistry();
        preRegistered.Set<TestEntity>(o => o.CaptureIndexMetrics = true);
        services.AddSingleton(preRegistered);

        _ = new CosmosRegistrationHelper(services, BuildConfig());

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<CosmosDiagnosticsRegistry>();

        Assert.Same(preRegistered, registry);
        Assert.True(registry.GetFor<TestEntity>().CaptureIndexMetrics);
    }

    [Fact]
    public void Repository_CtorHonorsPerEntityFlag_FromRegistry()
    {
        var registry = new CosmosDiagnosticsRegistry();
        registry.Set<TestEntity>(o => o.CaptureIndexMetrics = true);

        var repo = BuildRepo(registry);

        Assert.True(repo.CaptureIndexMetricsValue);
    }

    [Fact]
    public void Repository_CtorWithoutRegistry_LeavesFlagOff()
    {
        var repo = BuildRepo(diagnosticsRegistry: null);

        Assert.False(repo.CaptureIndexMetricsValue);
    }

    [Fact]
    public void Repository_CtorWithDifferentEntity_UsesDefault()
    {
        // Configuring an unrelated entity must NOT affect this repository.
        var registry = new CosmosDiagnosticsRegistry();
        registry.Set<OtherTestEntity>(o => o.CaptureIndexMetrics = true);

        var repo = BuildRepo(registry);

        Assert.False(repo.CaptureIndexMetricsValue);
    }

    private static FlagInspectingRepository BuildRepo(CosmosDiagnosticsRegistry? diagnosticsRegistry)
    {
        var options = Options.Create(new CosmosRepositoryOptions<TestEntity>
        {
            ContainerName = "Test",
            DatabaseName = "Test",
            PartitionKey = "/tenantId,/entityType",
            UseHierarchicalPartitionKey = true,
            WithCreateStructures = false
        });

        var fakeAccountKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("fakeAccountKey"));
        var client = new CosmosClient("https://example.com", fakeAccountKey);

        return new FlagInspectingRepository(
            options, client, NullLogger<FlagInspectingRepository>.Instance,
            sink: null, diagnosticsRegistry: diagnosticsRegistry);
    }

    private sealed class FlagInspectingRepository : CosmosTenantItemRepository<TestEntity>
    {
        public FlagInspectingRepository(
            IOptions<CosmosRepositoryOptions<TestEntity>> options,
            CosmosClient client,
            Microsoft.Extensions.Logging.ILogger<FlagInspectingRepository> logger,
            ICosmosQueryLogSink? sink = null,
            CosmosDiagnosticsRegistry? diagnosticsRegistry = null)
            : base(options, client, logger, sink, diagnosticsRegistry)
        {
        }

        public bool CaptureIndexMetricsValue => _captureIndexMetrics;
    }
}
