using MongoDB.Driver;
using MongoDbConsoleApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDbConsoleApp.Services
{
    public class OrderService
    {
        private readonly IMongoCollection<Order> _orderCollection;
        private readonly IMongoCollection<Product> _productCollection;

        public OrderService(MongoDbService mongoDbService)
        {
            _orderCollection = mongoDbService.GetCollection<Order>("Orders");
            _productCollection = mongoDbService.GetCollection<Product>("Products");
        }

        // Create a new order
        public async Task<Order> CreateOrderAsync(Order order)
        {
            // Validate if all products are active before creating the order
            foreach (var item in order.Items)
            {
                var product = await _productCollection.Find(p => p.Id == item.ProductId).FirstOrDefaultAsync();
                if (product == null || !product.IsActive)
                {
                    throw new InvalidOperationException($"Product {item.ProductId} is not available.");
                }
            }

            await _orderCollection.InsertOneAsync(order);
            return order;
        }

        // Request order cancellation (customer)
        public async Task<Order?> RequestCancelOrderAsync(string orderId, string cancellationNote)
        {
            var order = await FindOrderByIdAsync(orderId);
            if (order != null)
            {
                order.Status = OrderStatus.CancellationRequested;
                order.CancellationNote = cancellationNote;
                await UpdateOrderAsync(orderId, order);
            }
            return order;
        }

        // Approve order cancellation (CSR/Admin)
        public async Task<Order?> ApproveCancelOrderAsync(string orderId)
        {
            var order = await FindOrderByIdAsync(orderId);
            if (order != null)
            {
                order.Status = OrderStatus.Canceled;
                await UpdateOrderAsync(orderId, order);
            }
            return order;
        }

        // Notify customer about cancellation approval (dummy method, implement actual notification)
        public async Task NotifyCustomerAsync(string userId, string cancellationNote)
        {
            // Placeholder for notification logic (e.g., email or push notification)
            await Task.CompletedTask;
        }

        // Find an order by ID
        public async Task<Order?> FindOrderByIdAsync(string orderId)
        {
            return await _orderCollection.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        }

        // Find orders by user ID
        public async Task<List<Order>> FindOrdersByUserIdAsync(string userId)
        {
            return await _orderCollection.Find(o => o.UserId == userId).ToListAsync();
        }

        // Mark order item as delivered by vendor
        public async Task<Order?> MarkOrderItemAsDeliveredAsync(string orderId, string vendorId, string productId)
        {
            var order = await FindOrderByIdAsync(orderId);
            if (order == null)
            {
                // Log or return error
                return null;
            }

            // Find the item to update
            var item = order.Items.FirstOrDefault(i => i.ProductId == productId && i.VendorId == vendorId);

            if (item != null)
            {
                // Update the status of the item
                item.Status = OrderItemStatus.Delivered;
                await UpdateOrderAsync(orderId, order); // Save changes to the order

                // After item status update, update the order status based on items delivery status
                UpdateOrderStatus(order);
                await UpdateOrderAsync(orderId, order); // Save changes to order status
            }
            else
            {
                // Log or handle case where product or vendor doesn't match
                return null;
            }

            return order;
        }


        // Update the entire order status based on item delivery status
        private void UpdateOrderStatus(Order order)
        {
            if (order.Items.All(i => i.Status == OrderItemStatus.Delivered))
            {
                order.Status = OrderStatus.Delivered; // All items delivered
            }
            else if (order.Items.Any(i => i.Status == OrderItemStatus.Delivered))
            {
                order.Status = OrderStatus.PartiallyDelivered; // At least one item delivered
            }
            else
            {
                order.Status = OrderStatus.Processing; // No items delivered
            }
        }

        // Mark the entire order as delivered by CSR or Administrator
        public async Task<Order?> MarkOrderAsDeliveredByCSRAsync(string orderId)
        {
            var order = await FindOrderByIdAsync(orderId);
            if (order == null || !order.IsFullyDelivered())
            {
                return null; // If order is null or not fully delivered
            }

            order.Status = OrderStatus.Delivered;
            await UpdateOrderAsync(orderId, order);
            return order;
        }

        // Update an existing order
        public async Task<Order?> UpdateOrderAsync(string orderId, Order updatedOrder)
        {
            var result = await _orderCollection.ReplaceOneAsync(o => o.Id == orderId, updatedOrder);
            return result.IsAcknowledged ? updatedOrder : null;
        }
    }
}
