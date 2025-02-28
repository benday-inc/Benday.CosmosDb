namespace Benday.CosmosDb.Utilities;

public class CosmosConfig
{
    public CosmosConfig(string accountKey, string endpoint, string databaseName, string containerName, string partitionKey, bool createStructures)
    {
        AccountKey = accountKey;
        Endpoint = endpoint;
        DatabaseName = databaseName;
        ContainerName = containerName;
        PartitionKey = partitionKey;
        CreateStructures = createStructures;
    }

    public string AccountKey { get; set; }
    public string Endpoint { get; set; }
    public string DatabaseName { get; set; }
    public string ContainerName { get; set; }
    public string PartitionKey { get; set; }
    public bool CreateStructures { get; set; }


    public string ConnectionString
    {
        get
        {
            return $"AccountEndpoint={Endpoint};AccountKey={AccountKey};";
        }
    }
}
