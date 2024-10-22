using MongoDB.Driver;
using MongoDbConsoleApp.Models;
using System;
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

        // Create a new order and reduce stock
        public async Task<Order> CreateOrderAsync(Order order)
        {
            // Validate if all products are active and have enough stock
            foreach (var item in order.Items)
            {
                var product = await GetProductByIdAsync(item.ProductId);
                if (product == null || !product.IsActive)
                {
                    throw new InvalidOperationException($"Product {item.ProductId} is not available.");
                }

                // Check if stock is sufficient
                if (product.Stock < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for product {product.Name}.");
                }
            }

            // Reduce stock for all items only after validation
            foreach (var item in order.Items)
            {
                var product = await GetProductByIdAsync(item.ProductId);
                // product.Stock -= item.Quantity;
                var update = Builders<Product>.Update.Set(p => p.Stock, product.Stock);
                await _productCollection.UpdateOneAsync(p => p.Id == product.Id, update);
            }

            // Insert the new order after stock adjustment
            await _orderCollection.InsertOneAsync(order);
            return order;
        }

        // Request order cancellation (customer)
        public async Task<Order?> RequestCancelOrderAsync(string orderId, string cancellationNote)
        {
            var order = await FindOrderByIdAsync(orderId);
            if (order == null)
            {
                return null; // Order not found
            }

            if (order.Status != OrderStatus.Processing)
            {
                throw new InvalidOperationException("Only orders in processing can be requested for cancellation.");
            }

            order.Status = OrderStatus.CancellationRequested;
            order.CancellationNote = cancellationNote;
            await UpdateOrderAsync(orderId, order);
            return order;
        }

        // Approve order cancellation (CSR/Admin)
        public async Task<Order?> ApproveCancelOrderAsync(string orderId)
        {
            var order = await FindOrderByIdAsync(orderId);
            if (order == null)
            {
                return null; // Order not found
            }
            foreach (var item in order.Items)
            {
                var product = await GetProductByIdAsync(item.ProductId);
                product.Stock += item.Quantity;
                var update = Builders<Product>.Update.Set(p => p.Stock, product.Stock);
                await _productCollection.UpdateOneAsync(p => p.Id == product.Id, update);
            }
            order.Status = OrderStatus.Canceled;
            await UpdateOrderAsync(orderId, order);
            return order;
        }

        // Notify customer about cancellation approval
        public async Task NotifyCustomerAsync(string? userId, string? cancellationNote)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(cancellationNote))
            {
                throw new ArgumentException("User ID and cancellation note must be provided.");
            }

            // Placeholder for notification logic (e.g., email or push notification)
            await Task.CompletedTask;
        }

        // Get a product by ID
        public async Task<Product?> GetProductByIdAsync(string productId)
        {
            return await _productCollection.Find(p => p.Id == productId).FirstOrDefaultAsync();
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
                return null; // Order not found
            }

            // Find the item to update
            var item = order.Items.FirstOrDefault(i => i.ProductId == productId && i.VendorId == vendorId);
            if (item != null)
            {
                // Update the status of the item
                item.Status = OrderItemStatus.Delivered;

                // Save changes to the order
                await UpdateOrderAsync(orderId, order);

                // Update the order status based on items' delivery status
                UpdateOrderStatus(order);
                await UpdateOrderAsync(orderId, order); // Save changes to order status
            }
            else
            {
                // Log or handle case where product or vendor doesn't match
                return null; // Item not found
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

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _orderCollection.Find(_ => true).ToListAsync(); // Fetch all orders
        }

        public async Task<List<Order>> FindOrdersByVendorIdAsync(string vendorId)
        {
            // Filter orders where at least one item has the specified VendorId
            return await _orderCollection.Find(order => order.Items.Any(item => item.VendorId == vendorId)).ToListAsync();
        }




    }
}
