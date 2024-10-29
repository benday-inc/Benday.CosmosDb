using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.Utilities;
using Newtonsoft.Json;

namespace Benday.CosmosDb.DomainModels;

public abstract class CosmosIdentityBase : IOwnedItem
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("pk")]
    public abstract string PartitionKey { get; set; }
    public virtual string OwnerId { get; set; } = string.Empty;

    [JsonProperty("_ts")]
    [JsonConverter(typeof(UnixDateTimeConverter))]
    public DateTime Timestamp { get; set; }

    [JsonProperty("_etag")]
    public string Etag { get; set; } = string.Empty;

    [JsonProperty(CosmosDbConstants.DiscriminatorPropertyName)]
    public virtual string DiscriminatorValue
    {
        get
        {
            return GetDiscriminatorName();
        }
        set
        {

        }
    }
    protected abstract string GetDiscriminatorName();
}
