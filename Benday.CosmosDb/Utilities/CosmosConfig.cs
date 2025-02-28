namespace Benday.CosmosDb.Utilities;

public class CosmosConfig
{
    public required string AccountKey { get; set; }
    public required string Endpoint { get; set; }
    public required string DatabaseName { get; set; }
    public required string ContainerName { get; set; }
    public required string PartitionKey { get; set; }
    public required bool CreateStructures { get; set; }


    public string ConnectionString
    {
        get
        {
            return $"AccountEndpoint={Endpoint};AccountKey={AccountKey};";
        }
    }
}
