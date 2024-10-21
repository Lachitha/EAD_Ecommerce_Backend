using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MongoDbConsoleApp.Models
{
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = OrderStatus.Processing;
        public decimal Total { get; set; }
        public string? CancellationNote { get; set; }

        public bool IsFullyDelivered()
        {
            return Items.All(i => i.Status == OrderItemStatus.Delivered);
        }
    }

    public class OrderItem
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; }

        public string Description { get; set; }

        public string ImageBase64 { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? VendorId { get; set; }
        public string Status { get; set; } = OrderItemStatus.Pending;
    }

    public static class OrderItemStatus
    {
        public const string Pending = "Pending";
        public const string Ready = "Ready";
        public const string Delivered = "Delivered";
    }

    public static class OrderStatus
    {
        public const string Processing = "Processing";
        public const string PartiallyDelivered = "Partially Delivered";
        public const string Delivered = "Delivered";
        public const string Canceled = "Canceled";
        public const string CancellationRequested = "Cancellation Requested";
    }
}
