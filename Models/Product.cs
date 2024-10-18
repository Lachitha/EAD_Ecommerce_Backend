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

        public int Stock { get; set; } // Stock will be managed automatically based on quantity

        public bool IsActive { get; set; } = false; // To activate/deactivate the product

        public int LowStockThreshold { get; set; } // Alert vendor if stock goes below this threshold

        public string VendorId { get; set; } = string.Empty; // Associate product with a vendor

        // Embedded ProductCategory object
        public List<ProductCategoryDetails> Categories { get; set; } = new List<ProductCategoryDetails>();

        public string? ImageBase64 { get; set; } // Base64 string to store image

        public class ProductCategoryDetails
        {
            public object Name { get; internal set; }
            public object Description { get; internal set; }
            public string Id { get; internal set; }
        }
    }



    public class ProductCategoryDetails
    {
        public string Id { get; set; } // New property
        public string Name { get; set; } // New property
        public string Description { get; set; } // New property
    }



}

