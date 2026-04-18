using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Benday.CosmosDb.UnitTests.Diagnostics;

/// <summary>
/// Captures log entries so tests can assert on level + rendered text.
/// </summary>
internal sealed class TestLogger : ILogger, ILogger<DiagnosticsTestRepository>, ILogger<LoggingDiagnosticsTestRepository>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
