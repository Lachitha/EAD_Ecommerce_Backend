using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbConsoleApp.Models
{
    public enum ProductCategory
    {
        MensWear,
        KidsWear,
        WomenWear,
        Toys,
        BabyWear
    }

    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; } // New property for product quantity

        public int Stock { get; set; } // Stock will be managed automatically based on quantity

        public bool IsActive { get; set; } = false; // To activate/deactivate the product

        public int LowStockThreshold { get; set; } // Alert vendor if stock goes below this threshold

        public string VendorId { get; set; } = string.Empty; // Associate product with a vendor

        [BsonRepresentation(BsonType.String)] // Store enum as string in MongoDB
        public ProductCategory Category { get; set; } = ProductCategory.MensWear; // Default category

        public string? ImageBase64 { get; set; } // Base64 string to store image
    }
}
