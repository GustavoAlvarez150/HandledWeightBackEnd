namespace HandledWeight.Entities
{
    public class Drivers
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string LicenseNumber { get; set; }
        public string? Address { get; set; } = string.Empty;
        public string? State { get; set; } = string.Empty;
        public string? city { get; set; } = string.Empty;
        public string? Phone { get; set; } = string.Empty;
        public string? Phone1 { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
