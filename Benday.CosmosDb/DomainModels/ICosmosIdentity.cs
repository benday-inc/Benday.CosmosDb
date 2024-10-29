using Benday.CosmosDb.Repositories;

namespace Benday.CosmosDb.DomainModels;

using Benday.CosmosDb.Utilities;
using Newtonsoft.Json;

public interface ICosmosIdentity
{
    [JsonProperty("id")]
    string Id { get; set; }

    [JsonProperty("pk")]
    string PartitionKey { get; set; }

    [JsonProperty("_ts")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime Timestamp { get; set; }

    [JsonProperty("_etag")]
    public string Etag { get; set; }

    [JsonProperty(CosmosDbConstants.DiscriminatorPropertyName)]
    string DiscriminatorValue
    {
        get;
    }
}
