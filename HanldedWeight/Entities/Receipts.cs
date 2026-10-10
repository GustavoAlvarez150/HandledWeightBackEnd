using System;
using Microsoft.EntityFrameworkCore;

namespace HandledWeight.Entities
{
    public class Receipts
    {
        public int Id { get; set; }
        public string? ReceiptNumber { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public int DriverId { get; set; }
        public int ProductId { get; set; }
        public int BussinesPartnerId { get; set; } // FK to BussinesPartners


        // New weight / pricing / location / misc fields
        [Precision(18, 2)]
        public decimal? WeightIn { get; set; } = 0;

        [Precision(18, 2)]
        public decimal? WeightOut { get; set; } = 0;

        [Precision(18, 2)]
        public decimal? Price { get; set; }  = 0;
        [Precision(18, 2)]
        public decimal TotalAmount { get; set; }

        public string? Origin { get; set; } 
        public string? Destination { get; set; }
        public string? OCity { get; set; }
        public string? DCity { get; set; }
        public string? Observations { get; set; }
        public Boolean IsPublic { get; set; } = false;

        [Precision(18, 4)]
        public decimal? FitFactor { get; set; }

        [Precision(18, 2)]
        public decimal? GrossWeight { get; set; }

        [Precision(18, 2)]
        public decimal? Tara { get; set; }

        [Precision(18, 2)]
        public decimal? TotalWeight { get; set; }

        [Precision(18, 2)]
        public decimal? TotalWeightAdjust { get; set; }


        // Navigation properties
        public Drivers? Driver { get; set; }
        public Products? Product { get; set; }
        public BussinesPartners? BussinesPartner { get; set; }
        public Vehicle? Vehicle { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
