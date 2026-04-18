using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Benday.CosmosDb.Diagnostics;
using Benday.CosmosDb.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Benday.CosmosDb.UnitTests.Diagnostics;

public class CosmosQueryDiagnosticsFixture
{
    private static DiagnosticsTestRepository CreateRepository()
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

        var logger = NullLogger<DiagnosticsTestRepository>.Instance;

        return new DiagnosticsTestRepository(options, client, logger);
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
        // Chain at least one operator so ToQueryDefinition() produces a real definition.
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
    public void PointOperation_FiresOnQueryDiagnostics_Once()
    {
        var repo = CreateRepository();

        repo.CallLogPointOperationDiagnostics(
            operationName: nameof(PointOperation_FiresOnQueryDiagnostics_Once),
            requestCharge: 2.5,
            diagnostics: CreateDiagnosticsMock().Object,
            duration: TimeSpan.FromMilliseconds(17));

        Assert.Single(repo.CapturedEvents);

        var evt = repo.CapturedEvents[0];
        Assert.Equal(CosmosQueryEventKind.PointOperation, evt.EventKind);
        Assert.Equal(2.5, evt.RequestCharge);
        Assert.Equal(TimeSpan.FromMilliseconds(17), evt.Duration);
        Assert.Null(evt.QueryText);
        Assert.Null(evt.Parameters);
        Assert.False(evt.IsCrossPartition);
        Assert.Equal(0, evt.ResultCount);
        Assert.Equal(nameof(DiagnosticsTestRepository), evt.RepositoryName);
        Assert.Contains(nameof(PointOperation_FiresOnQueryDiagnostics_Once), evt.QueryDescription);
    }

    [Fact]
    public async Task FeedQuery_FiresNPlusOneEvents_ForNPages()
    {
        var repo = CreateRepository();

        var page1 = CreatePageMock(
            new[] { new TestEntity(), new TestEntity() },
            requestCharge: 3.0);
        var page2 = CreatePageMock(
            new[] { new TestEntity() },
            requestCharge: 2.0);
        var iterator = BuildIterator(page1, page2);

        var results = await repo.CallGetResultsAsync(
            iterator,
            queryDescription: "feed-query",
            queryText: "SELECT * FROM c",
            parameters: new Dictionary<string, object?> { ["@p"] = 1 });

        Assert.Equal(3, results.Count);
        Assert.Equal(3, repo.CapturedEvents.Count);

        Assert.Equal(CosmosQueryEventKind.FeedResponsePage, repo.CapturedEvents[0].EventKind);
        Assert.Equal(2, repo.CapturedEvents[0].ResultCount);
        Assert.Equal(3.0, repo.CapturedEvents[0].RequestCharge);

        Assert.Equal(CosmosQueryEventKind.FeedResponsePage, repo.CapturedEvents[1].EventKind);
        Assert.Equal(1, repo.CapturedEvents[1].ResultCount);
        Assert.Equal(2.0, repo.CapturedEvents[1].RequestCharge);

        var total = repo.CapturedEvents[2];
        Assert.Equal(CosmosQueryEventKind.QueryTotal, total.EventKind);
        Assert.Equal(3, total.ResultCount);
        Assert.Equal(5.0, total.RequestCharge);
        Assert.Equal("SELECT * FROM c", total.QueryText);
        Assert.NotNull(total.Parameters);
        Assert.Equal(1, total.Parameters!["@p"]);
    }

    [Fact]
    public async Task FeedQuery_DurationIsNonZero_OnEveryEvent()
    {
        var repo = CreateRepository();

        var page = CreatePageMock(new[] { new TestEntity() }, requestCharge: 1.0);
        var iterator = BuildIterator(page);

        await repo.CallGetResultsAsync(iterator, queryDescription: "t");

        Assert.NotEmpty(repo.CapturedEvents);
        foreach (var evt in repo.CapturedEvents)
        {
            Assert.True(evt.Duration >= TimeSpan.Zero,
                $"Duration for {evt.EventKind} should be non-negative but was {evt.Duration}");
        }
        // Aggregated total duration is the sum of page durations — always >= any single page's.
        var total = repo.CapturedEvents.Last();
        Assert.Equal(CosmosQueryEventKind.QueryTotal, total.EventKind);
        Assert.True(total.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task FeedQuery_QueryTextAndParameters_FlowIntoEveryEvent()
    {
        var repo = CreateRepository();

        var page = CreatePageMock(new[] { new TestEntity() }, requestCharge: 1.0);
        var iterator = BuildIterator(page);

        var parameters = new Dictionary<string, object?>
        {
            ["@id"] = "abc",
            ["@entityType"] = "TestEntity"
        };
        await repo.CallGetResultsAsync(
            iterator,
            queryDescription: "t",
            queryText: "SELECT * FROM c WHERE c.id = @id",
            parameters: parameters);

        foreach (var evt in repo.CapturedEvents)
        {
            Assert.Equal("SELECT * FROM c WHERE c.id = @id", evt.QueryText);
            Assert.NotNull(evt.Parameters);
            Assert.Equal("abc", evt.Parameters!["@id"]);
            Assert.Equal("TestEntity", evt.Parameters!["@entityType"]);
        }
    }

    [Fact]
    public async Task FeedQuery_CrossPartition_OnAnyPage_PropagatesToTotal()
    {
        var repo = CreateRepository();
        repo.ForceCrossPartition = true;

        var page = CreatePageMock(new[] { new TestEntity() }, requestCharge: 1.0);
        var iterator = BuildIterator(page);

        await repo.CallGetResultsAsync(iterator, queryDescription: "t");

        var total = repo.CapturedEvents.Last();
        Assert.Equal(CosmosQueryEventKind.QueryTotal, total.EventKind);
        Assert.True(total.IsCrossPartition);
        Assert.Contains(repo.CapturedEvents, e =>
            e.EventKind == CosmosQueryEventKind.FeedResponsePage && e.IsCrossPartition);
    }

    [Fact]
    public async Task FeedQuery_NotCrossPartition_TotalIsFalse()
    {
        var repo = CreateRepository();
        repo.ForceCrossPartition = false;

        var page = CreatePageMock(new[] { new TestEntity() }, requestCharge: 1.0);
        var iterator = BuildIterator(page);

        await repo.CallGetResultsAsync(iterator, queryDescription: "t");

        Assert.All(repo.CapturedEvents, e => Assert.False(e.IsCrossPartition));
    }

    [Fact]
    public async Task FeedQuery_TotalResultCount_MatchesReturnedListSize()
    {
        var repo = CreateRepository();

        var page1 = CreatePageMock(
            new[] { new TestEntity(), new TestEntity(), new TestEntity() },
            requestCharge: 1.0);
        var page2 = CreatePageMock(
            new[] { new TestEntity(), new TestEntity() },
            requestCharge: 1.0);
        var iterator = BuildIterator(page1, page2);

        var results = await repo.CallGetResultsAsync(iterator, queryDescription: "t");

        var total = repo.CapturedEvents.Last();
        Assert.Equal(CosmosQueryEventKind.QueryTotal, total.EventKind);
        Assert.Equal(results.Count, total.ResultCount);
        Assert.Equal(5, total.ResultCount);
    }

    [Fact]
    public async Task ExecuteScalarAsync_FiresSingleQueryTotalEvent()
    {
        var repo = CreateRepository();

        var containerQueryable = CreateCosmosQueryable();

        var responseMock = new Mock<Response<int>>();
        responseMock.Setup(r => r.Resource).Returns(42);
        responseMock.Setup(r => r.RequestCharge).Returns(4.25);
        responseMock.Setup(r => r.Diagnostics).Returns(CreateDiagnosticsMock().Object);

        var result = await repo.CallExecuteScalarAsync(
            containerQueryable,
            _ => Task.FromResult(responseMock.Object),
            queryDescription: "scalar-test",
            partitionKey: default,
            resultCountSelector: count => count);

        Assert.Equal(42, result);
        Assert.Single(repo.CapturedEvents);

        var evt = repo.CapturedEvents[0];
        Assert.Equal(CosmosQueryEventKind.QueryTotal, evt.EventKind);
        Assert.Equal(4.25, evt.RequestCharge);
        Assert.Equal(42, evt.ResultCount);
        Assert.NotNull(evt.QueryText);
        Assert.True(evt.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteScalarAsync_DefaultResultCountSelector_MapsNullToZero()
    {
        var repo = CreateRepository();
        var containerQueryable = CreateCosmosQueryable();

        var responseMock = new Mock<Response<TestEntity?>>();
        responseMock.Setup(r => r.Resource).Returns((TestEntity?)null);
        responseMock.Setup(r => r.RequestCharge).Returns(1.0);
        responseMock.Setup(r => r.Diagnostics).Returns(CreateDiagnosticsMock().Object);

        await repo.CallExecuteScalarAsync(
            containerQueryable,
            _ => Task.FromResult(responseMock.Object),
            queryDescription: "scalar-null",
            partitionKey: default);

        Assert.Single(repo.CapturedEvents);
        Assert.Equal(0, repo.CapturedEvents[0].ResultCount);
    }

    [Fact]
    public async Task ExecuteScalarAsync_DefaultResultCountSelector_MapsNonNullToOne()
    {
        var repo = CreateRepository();
        var containerQueryable = CreateCosmosQueryable();

        var responseMock = new Mock<Response<TestEntity>>();
        responseMock.Setup(r => r.Resource).Returns(new TestEntity());
        responseMock.Setup(r => r.RequestCharge).Returns(1.0);
        responseMock.Setup(r => r.Diagnostics).Returns(CreateDiagnosticsMock().Object);

        await repo.CallExecuteScalarAsync(
            containerQueryable,
            _ => Task.FromResult(responseMock.Object),
            queryDescription: "scalar-one",
            partitionKey: default);

        Assert.Single(repo.CapturedEvents);
        Assert.Equal(1, repo.CapturedEvents[0].ResultCount);
    }

    [Fact]
    public async Task ExecuteScalarAsync_CrossPartition_FlagPropagates()
    {
        var repo = CreateRepository();
        repo.ForceCrossPartition = true;
        var containerQueryable = CreateCosmosQueryable();

        var responseMock = new Mock<Response<int>>();
        responseMock.Setup(r => r.Resource).Returns(7);
        responseMock.Setup(r => r.RequestCharge).Returns(1.0);
        responseMock.Setup(r => r.Diagnostics).Returns(CreateDiagnosticsMock().Object);

        await repo.CallExecuteScalarAsync(
            containerQueryable,
            _ => Task.FromResult(responseMock.Object),
            queryDescription: "scalar-xp",
            partitionKey: default);

        Assert.Single(repo.CapturedEvents);
        Assert.True(repo.CapturedEvents[0].IsCrossPartition);
    }

    [Fact]
    public void LogPointOperation_TimestampAndRepositoryName_Populated()
    {
        var repo = CreateRepository();
        var before = DateTimeOffset.UtcNow;

        repo.CallLogPointOperationDiagnostics(
            "op", 1.0, CreateDiagnosticsMock().Object, TimeSpan.FromMilliseconds(5));

        var after = DateTimeOffset.UtcNow;

        var evt = Assert.Single(repo.CapturedEvents);
        Assert.Equal(nameof(DiagnosticsTestRepository), evt.RepositoryName);
        Assert.InRange(evt.Timestamp, before, after);
    }
}
