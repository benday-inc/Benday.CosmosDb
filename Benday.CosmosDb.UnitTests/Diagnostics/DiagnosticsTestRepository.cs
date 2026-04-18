using System.Collections.Generic;
using System.Threading.Tasks;
using Benday.CosmosDb.Diagnostics;
using Benday.CosmosDb.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.CosmosDb.UnitTests.Diagnostics;

/// <summary>
/// Test double that captures every <see cref="CosmosQueryDiagnostics"/> event
/// emitted by the base repository and exposes the protected diagnostic hooks
/// for direct invocation from tests.
/// </summary>
public class DiagnosticsTestRepository : CosmosTenantItemRepository<TestEntity>
{
    public DiagnosticsTestRepository(
        IOptions<CosmosRepositoryOptions<TestEntity>> options,
        CosmosClient client,
        ILogger<DiagnosticsTestRepository> logger) :
        base(options, client, logger)
    {
    }

    public List<CosmosQueryDiagnostics> CapturedEvents { get; } = [];

    public bool ForceCrossPartition { get; set; }

    protected override void OnQueryDiagnostics(CosmosQueryDiagnostics diagnostics)
    {
        CapturedEvents.Add(diagnostics);
    }

    protected override bool IsCrossPartitionQuery(CosmosDiagnostics diagnostics)
        => ForceCrossPartition;

    public void CallLogPointOperationDiagnostics(
        string operationName, double requestCharge,
        CosmosDiagnostics diagnostics, System.TimeSpan duration)
        => LogPointOperationDiagnostics(operationName, requestCharge, diagnostics, duration);

    public bool CallLogFeedResponseDiagnostics(
        string queryDescription, double requestCharge,
        CosmosDiagnostics diagnostics, System.TimeSpan duration,
        int resultCount, string? queryText,
        IReadOnlyDictionary<string, object?>? parameters,
        PartitionKey partitionKey)
        => LogFeedResponseDiagnostics(
            queryDescription, requestCharge, diagnostics, duration,
            resultCount, queryText, parameters, partitionKey);

    public void CallLogQueryTotalDiagnostics(
        string queryDescription, double totalRequestCharge,
        System.TimeSpan totalDuration, int totalResultCount,
        string? queryText,
        IReadOnlyDictionary<string, object?>? parameters,
        PartitionKey partitionKey, bool isCrossPartition)
        => LogQueryTotalDiagnostics(
            queryDescription, totalRequestCharge, totalDuration,
            totalResultCount, queryText, parameters, partitionKey, isCrossPartition);

    public Task<List<TestEntity>> CallGetResultsAsync(
        FeedIterator<TestEntity> iterator, string queryDescription,
        string? queryText = null,
        IReadOnlyDictionary<string, object?>? parameters = null,
        PartitionKey partitionKey = default)
        => GetResultsAsync(iterator, queryDescription, queryText, parameters, partitionKey);

    public Task<TResult> CallExecuteScalarAsync<TResult>(
        System.Linq.IQueryable<TestEntity> query,
        System.Func<System.Linq.IQueryable<TestEntity>, Task<Response<TResult>>> operation,
        string queryDescription,
        PartitionKey partitionKey,
        System.Func<TResult, int>? resultCountSelector = null)
        => ExecuteScalarAsync(query, operation, queryDescription, partitionKey, resultCountSelector);
}
