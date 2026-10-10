using System.ComponentModel.DataAnnotations.Schema;

namespace HandledWeight.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; }
        public string? Branch { get; set; }
        public string? KindOfVehicle { get; set; }

        [NotMapped]
        public string? Type
        {
            get => KindOfVehicle;
            set => KindOfVehicle = value;
        }

        public string Plate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
