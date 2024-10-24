using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.IdGenerators;

namespace backend.Models
{
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } // Nullable string

        public string? Type { get; set; } // Nullable string

        public string? DateOfOrder { get; set; } // Nullable string

        public string? Notes { get; set; } // Nullable string

        public int? OrderNumber { get; set; } // Nullable int

        public string? Status { get; set; } // Nullable string

        public List<Item>? Items { get; set; } // Nullable List<Item>

        public string? TableNumber { get; set; } // Nullable string

        public double? TotoalPrice { get; set; } // Nullable double

        public int? GrossNumber { get; set; } // Nullable int

        public string? PaymentType { get; set; } // Nullable string

        public double? DeleiveryCharge { get; set; } // Nullable double

        public string? Location { get; set; } // Nullable string

        public string? Created_by { get; set; } // Nullable string

        public int? Quantity { get; set; } // Nullable int

        public double? TotalOldPrice { get; set; } // Nullable double

        public CustomerNumber? CustomerNumber { get; set; } // Nullable CustomerNumber
    }
}
