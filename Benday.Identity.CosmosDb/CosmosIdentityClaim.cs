namespace Benday.Identity.CosmosDb
{
    public class CosmosIdentityClaim : SystemOwnedItem
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
