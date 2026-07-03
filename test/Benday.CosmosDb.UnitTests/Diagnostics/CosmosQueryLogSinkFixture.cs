using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Benday.CosmosDb.Diagnostics;
using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.Utilities;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Benday.CosmosDb.UnitTests.Diagnostics;

public class CosmosQueryLogSinkFixture
{
    private sealed class CapturingSink : ICosmosQueryLogSink
    {
        public List<CosmosQueryDiagnostics> Events { get; } = new();
        public void Record(CosmosQueryDiagnostics diagnostics) => Events.Add(diagnostics);
    }

    private sealed class ThrowingSink : ICosmosQueryLogSink
    {
        public int CallCount { get; private set; }
        public void Record(CosmosQueryDiagnostics diagnostics)
        {
            CallCount++;
            throw new InvalidOperationException("simulated sink failure");
        }
    }

    private static DiagnosticsTestRepository CreateRepository(ICosmosQueryLogSink? sink)
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

        return new DiagnosticsTestRepository(
            options, client, NullLogger<DiagnosticsTestRepository>.Instance, sink);
    }

    private static IQueryable<TestEntity> CreateCosmosQueryable()
    {
        var fakeAccountKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("fakeAccountKey"));
        var client = new CosmosClient("https://example.com", fakeAccountKey);
        var container = client.GetDatabase("Test").GetContainer("Test");
        var queryable = container.GetItemLinqQueryable<TestEntity>(
            allowSynchronousQueryExecution: true,
            linqSerializerOptions: new CosmosLinqSerializerOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.Default
            });
        return queryable.Where(x => x.EntityType == nameof(TestEntity));
    }

    private static Mock<CosmosDiagnostics> CreateDiagnosticsMock()
    {
        var mock = new Mock<CosmosDiagnostics>();
        mock.Setup(d => d.ToString()).Returns("{\"mock\":\"diagnostics\"}");
        return mock;
    }

    private static Mock<FeedResponse<TestEntity>> CreatePageMock(
        IReadOnlyList<TestEntity> items, double requestCharge)
    {
        var mock = new Mock<FeedResponse<TestEntity>>();
        mock.Setup(r => r.Count).Returns(items.Count);
        mock.Setup(r => r.RequestCharge).Returns(requestCharge);
        mock.Setup(r => r.Diagnostics).Returns(CreateDiagnosticsMock().Object);
        mock.Setup(r => r.GetEnumerator()).Returns(() => items.GetEnumerator());
        return mock;
    }

    private static FeedIterator<TestEntity> BuildIterator(
        params Mock<FeedResponse<TestEntity>>[] pageMocks)
    {
        var iterator = new Mock<FeedIterator<TestEntity>>();
        var queue = new Queue<FeedResponse<TestEntity>>(pageMocks.Select(p => p.Object));
        iterator.Setup(i => i.HasMoreResults).Returns(() => queue.Count > 0);
        iterator
            .Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(queue.Dequeue()));
        return iterator.Object;
    }

    [Fact]
    public async Task NoSinkProvided_NullFallsBackToNoOp_NoCrash()
    {
        var repo = CreateRepository(sink: null);

        var page = CreatePageMock(new[] { new TestEntity() }, requestCharge: 1.0);
        var iterator = BuildIterator(page);

        var results = await repo.CallGetResultsAsync(iterator, "t");
        Assert.Single(results);
    }

    [Fact]
    public void NoOpCosmosQueryLogSink_Record_DoesNothing()
    {
        NoOpCosmosQueryLogSink.Instance.Record(new CosmosQueryDiagnostics
        {
            EventKind = CosmosQueryEventKind.PointOperation
        });
    }

    [Fact]
    public void PointOperation_FiresSink()
    {
        var sink = new CapturingSink();
        var repo = CreateRepository(sink);

        repo.CallLogPointOperationDiagnostics(
            "op", 1.25, CreateDiagnosticsMock().Object, TimeSpan.FromMilliseconds(7));

        Assert.Single(sink.Events);
        Assert.Equal(CosmosQueryEventKind.PointOperation, sink.Events[0].EventKind);
        Assert.Equal(1.25, sink.Events[0].RequestCharge);
    }

    [Fact]
    public async Task FeedQuery_FiresSink_OncePerPagePlusTotal()
    {
        var sink = new CapturingSink();
        var repo = CreateRepository(sink);

        var page1 = CreatePageMock(
            new[] { new TestEntity(), new TestEntity() }, requestCharge: 3.0);
        var page2 = CreatePageMock(
            new[] { new TestEntity() }, requestCharge: 2.0);
        var iterator = BuildIterator(page1, page2);

        await repo.CallGetResultsAsync(iterator, "feed");

        Assert.Equal(3, sink.Events.Count);
        Assert.Equal(CosmosQueryEventKind.FeedResponsePage, sink.Events[0].EventKind);
        Assert.Equal(CosmosQueryEventKind.FeedResponsePage, sink.Events[1].EventKind);
        Assert.Equal(CosmosQueryEventKind.QueryTotal, sink.Events[2].EventKind);
        Assert.Equal(3, sink.Events[2].ResultCount);
        Assert.Equal(5.0, sink.Events[2].RequestCharge);
    }

    [Fact]
    public async Task ExecuteScalarAsync_FiresSink_WithQueryTotal()
    {
        var sink = new CapturingSink();
        var repo = CreateRepository(sink);

        var responseMock = new Mock<Response<int>>();
        responseMock.Setup(r => r.Resource).Returns(42);
        responseMock.Setup(r => r.RequestCharge).Returns(1.5);
        responseMock.Setup(r => r.Diagnostics).Returns(CreateDiagnosticsMock().Object);

        var result = await repo.CallExecuteScalarAsync(
            CreateCosmosQueryable(),
            _ => Task.FromResult(responseMock.Object),
            "scalar",
            partitionKey: default,
            resultCountSelector: count => count);

        Assert.Equal(42, result);
        Assert.Single(sink.Events);
        Assert.Equal(CosmosQueryEventKind.QueryTotal, sink.Events[0].EventKind);
        Assert.Equal(42, sink.Events[0].ResultCount);
        Assert.NotNull(sink.Events[0].QueryText);
    }

    [Fact]
    public async Task BothChannelsFire_TemplateHookAndSink()
    {
        var sink = new CapturingSink();
        var repo = CreateRepository(sink);

        var page = CreatePageMock(new[] { new TestEntity() }, requestCharge: 1.0);
        var iterator = BuildIterator(page);

        await repo.CallGetResultsAsync(iterator, "both");

        // DiagnosticsTestRepository captures OnQueryDiagnostics events in CapturedEvents.
        Assert.Equal(2, repo.CapturedEvents.Count); // 1 page + 1 total
        Assert.Equal(2, sink.Events.Count);          // same count on the sink
        for (int i = 0; i < repo.CapturedEvents.Count; i++)
        {
            Assert.Equal(repo.CapturedEvents[i].EventKind, sink.Events[i].EventKind);
            Assert.Equal(repo.CapturedEvents[i].RequestCharge, sink.Events[i].RequestCharge);
        }
    }

    [Fact]
    public async Task BrokenSink_DoesNotBreakQueries()
    {
        var sink = new ThrowingSink();
        var repo = CreateRepository(sink);

        var page = CreatePageMock(new[] { new TestEntity(), new TestEntity() }, requestCharge: 1.0);
        var iterator = BuildIterator(page);

        // Should not throw, and should still return the results.
        var results = await repo.CallGetResultsAsync(iterator, "broken");

        Assert.Equal(2, results.Count);
        Assert.True(sink.CallCount >= 1);
        // Template method still fired (sink throws AFTER OnQueryDiagnostics).
        Assert.Equal(2, repo.CapturedEvents.Count);
    }

    [Fact]
    public void BrokenSink_PointOperation_LoggedAsWarning()
    {
        var sink = new ThrowingSink();
        var logger = new TestLogger();

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

        var repo = new LoggingDiagnosticsTestRepository(options, client, logger, sink);

        repo.CallLogPointOperationDiagnostics(
            "op", 1.0, CreateDiagnosticsMock().Object, TimeSpan.Zero);

        Assert.Equal(1, sink.CallCount);
        Assert.Contains(logger.Entries, e =>
            e.Level == Microsoft.Extensions.Logging.LogLevel.Warning &&
            e.Message.Contains("ICosmosQueryLogSink.Record threw"));
    }

    [Fact]
    public void Di_Defaults_ResolveNoOpSink()
    {
        var services = new ServiceCollection();
        var config = new CosmosConfig
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
        _ = new CosmosRegistrationHelper(services, config);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<ICosmosQueryLogSink>();

        Assert.Same(NoOpCosmosQueryLogSink.Instance, resolved);
    }

    [Fact]
    public void Di_WithQueryLogSink_OverridesDefault()
    {
        var services = new ServiceCollection();
        var config = new CosmosConfig
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

        var helper = new CosmosRegistrationHelper(services, config);
        helper.WithQueryLogSink<CapturingSink>();

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<ICosmosQueryLogSink>();

        Assert.IsType<CapturingSink>(resolved);
    }

    [Fact]
    public void Di_ManualRegistrationBeforeHelper_Wins()
    {
        // Exercises the TryAddSingleton semantics — if a sink is registered
        // before CosmosRegistrationHelper runs, the helper's default no-op
        // registration must not override it.
        var services = new ServiceCollection();
        var preRegistered = new CapturingSink();
        services.AddSingleton<ICosmosQueryLogSink>(preRegistered);

        var config = new CosmosConfig
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
        _ = new CosmosRegistrationHelper(services, config);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<ICosmosQueryLogSink>();

        Assert.Same(preRegistered, resolved);
    }

    [Fact]
    public void Di_WithQueryLogSinkInstance_RegistersSpecificInstance()
    {
        var services = new ServiceCollection();
        var config = new CosmosConfig
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

        var helper = new CosmosRegistrationHelper(services, config);
        var sink = new CapturingSink();
        helper.WithQueryLogSink(sink);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<ICosmosQueryLogSink>();

        Assert.Same(sink, resolved);
    }
}
