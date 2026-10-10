using Org.BouncyCastle.Crypto;

namespace HandledWeight.Entities
{
    public class BussinesPartners
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? LastNames { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Cp { get; set; }
        public string RFC { get; set; }
        public string? TaxRegime { get; set; }
        public byte[]? ProofOfSit { get; set; }
        public bool IsClient { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
