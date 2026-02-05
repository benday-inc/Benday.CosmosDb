namespace Benday.Identity.CosmosDb
{
    public class CosmosIdentityRole : SystemOwnedItem
    {
        public string Name { get; set; } = string.Empty;

        public string NormalizedName
        {
            get
            {
                return Name.ToUpperInvariant();
            }
            set
            {
                // no-op: normalized value is computed from Name
            }
        }

        public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        public List<CosmosIdentityClaim> Claims { get; set; } = new List<CosmosIdentityClaim>();
    }
}
