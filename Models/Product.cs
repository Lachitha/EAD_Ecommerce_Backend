using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbConsoleApp.Models
{
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; } // New property for product quantity

        public bool IsActive { get; set; } = false; // To activate/deactivate the product

        public string VendorId { get; set; } = string.Empty; // Associate product with a vendor
    }
}
