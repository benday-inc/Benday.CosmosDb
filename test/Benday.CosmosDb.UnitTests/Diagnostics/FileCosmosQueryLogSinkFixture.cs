using System;
using System.IO;
using System.Text.Json;
using Benday.CosmosDb.Diagnostics;
using Xunit;

namespace Benday.CosmosDb.UnitTests.Diagnostics;

public class FileCosmosQueryLogSinkFixture
{
    private static string TempFile() =>
        Path.Combine(Path.GetTempPath(), "benday-cosmosdb-tests", Guid.NewGuid().ToString("N") + ".ndjson");

    private static CosmosQueryDiagnostics SampleEvent() => new()
    {
        EventKind = CosmosQueryEventKind.PointOperation,
        Timestamp = DateTimeOffset.UtcNow,
        RepositoryName = "CosmosTenantItemRepository`1",
        EntityType = "RequirementScore",
        QueryDescription = "SaveAsync",
        RequestCharge = 9.52,
        Duration = TimeSpan.FromMilliseconds(12),
    };

    [Fact]
    public void Record_WritesUtf8WithoutByteOrderMark()
    {
        var path = TempFile();

        using (var sink = new FileCosmosQueryLogSink(path))
        {
            sink.Record(SampleEvent());
        }

        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.Length > 3, "file should have content");
        Assert.False(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "file must not start with a UTF-8 BOM");
        Assert.Equal((byte)'{', bytes[0]);
    }

    [Fact]
    public void Record_EmitsEntityTypeField()
    {
        var path = TempFile();

        using (var sink = new FileCosmosQueryLogSink(path))
        {
            sink.Record(SampleEvent());
        }

        var line = Assert.Single(File.ReadAllLines(path));
        using var doc = JsonDocument.Parse(line);
        Assert.Equal("RequirementScore", doc.RootElement.GetProperty("entityType").GetString());
        Assert.Equal("PointOperation", doc.RootElement.GetProperty("eventKind").GetString());
    }
}
