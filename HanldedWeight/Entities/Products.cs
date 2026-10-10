using Microsoft.EntityFrameworkCore;

namespace HandledWeight.Entities
{
    public class Products
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        [Precision(18, 2)]
        public decimal? Price { get; set; }
        public string Unit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
