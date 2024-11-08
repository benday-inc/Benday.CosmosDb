using Benday.CosmosDb.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Benday.CosmosDb.UnitTests.Utilities;



public class UnixDateTimeConverterTests
{
    private readonly JsonSerializerOptions _options;

    public UnixDateTimeConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new UnixDateTimeConverter() }
        };
    }

    [Fact]
    public void Write_ShouldConvertDateTimeToUnixTimestamp()
    {
        // Arrange
        var dateTime = new DateTime(2023, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedUnixTime = ((DateTimeOffset)dateTime).ToUnixTimeSeconds();

        // Act
        var json = JsonSerializer.Serialize(dateTime, _options);

        // Assert
        Assert.Equal(expectedUnixTime.ToString(), json);
    }

    [Fact]
    public void Read_ShouldConvertUnixTimestampToDateTime()
    {
        // Arrange
        var unixTime = 1696118400L; // Unix timestamp for 2023-10-01 00:00:00 UTC
        var json = unixTime.ToString();
        var expectedDateTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;

        // Act
        var result = JsonSerializer.Deserialize<DateTime>(json, _options);

        // Assert
        Assert.Equal(expectedDateTime, result);
    }
}
