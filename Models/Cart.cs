using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace MongoDbConsoleApp.Models
{
    public class CartItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string ProductId { get; set; } = string.Empty;

        public int Quantity { get; set; } // Quantity of the product in the cart

        public decimal Price { get; set; } // Price at the time of adding to the cart

        public decimal Total => Quantity * Price; // Total price for this cart item
    }

    public class Cart
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string UserId { get; set; } = string.Empty; // User associated with the cart

        public List<CartItem> Items { get; set; } = new List<CartItem>(); // List of cart items

        public decimal TotalAmount => Items.Sum(item => item.Total); // Total amount of the cart
    }
}
