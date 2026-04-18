using System;
using Benday.CosmosDb.Diagnostics;
using Benday.CosmosDb.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Benday.CosmosDb.UnitTests.Diagnostics;

/// <summary>
/// Variant of <see cref="DiagnosticsTestRepository"/> that accepts an
/// <see cref="ILogger"/> directly, so tests can assert on log entries
/// emitted when a sink throws.
/// </summary>
internal sealed class LoggingDiagnosticsTestRepository : CosmosTenantItemRepository<TestEntity>
{
    public LoggingDiagnosticsTestRepository(
        IOptions<CosmosRepositoryOptions<TestEntity>> options,
        CosmosClient client,
        ILogger<LoggingDiagnosticsTestRepository> logger,
        ICosmosQueryLogSink? sink = null) :
        base(options, client, logger, sink)
    {
    }

    public void CallLogPointOperationDiagnostics(
        string operationName, double requestCharge,
        CosmosDiagnostics diagnostics, TimeSpan duration)
        => LogPointOperationDiagnostics(operationName, requestCharge, diagnostics, duration);
}
