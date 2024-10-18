using MongoDB.Driver;
using MongoDbConsoleApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDbConsoleApp.Services
{
    public class ProductService
    {
        private readonly IMongoCollection<Product> _products;

        public ProductService(MongoDbService mongoDbService)
        {
            _products = mongoDbService.GetCollection<Product>("Products");
        }

        public async Task CreateProductAsync(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product), "Product cannot be null.");

            product.Stock = product.Quantity; // Set initial stock based on quantity

            await _products.InsertOneAsync(product);

            // Check and notify if low stock after creation
            await CheckAndNotifyLowStockAsync(product);
        }

        public async Task<Product?> GetProductByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid product ID.", nameof(id));

            return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateProductAsync(string id, Product product)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid product ID.", nameof(id));

            if (product == null)
                throw new ArgumentNullException(nameof(product), "Product cannot be null.");

            product.Id = id; // Ensure the product ID matches the one being updated

            var existingProduct = await GetProductByIdAsync(id);
            if (existingProduct == null)
                throw new KeyNotFoundException($"No product found with ID: {id}");

            // Update the stock based on the new quantity
            product.Stock = existingProduct.Stock - (existingProduct.Quantity - product.Quantity);
            product.Quantity = product.Quantity; // Optionally update quantity

            var result = await _products.ReplaceOneAsync(p => p.Id == id, product);
            if (result.ModifiedCount == 0)
                throw new KeyNotFoundException($"No product found with ID: {id}");

            // Check and notify if low stock after update
            await CheckAndNotifyLowStockAsync(product);
        }

        private async Task CheckAndNotifyLowStockAsync(Product product)
        {
            // Check if the stock is below the low stock threshold
            if (product.Stock < product.LowStockThreshold)
            {
                await NotifyVendorLowStockAsync(product);
            }
        }

        private async Task NotifyVendorLowStockAsync(Product product)
        {
            // Notify the vendor of the low stock (this could be done via email or system notification)
            Console.WriteLine($"Low stock alert: Product '{product.Name}' is below the low stock threshold. Current stock: {product.Stock}");
            await Task.CompletedTask; // Simulate async operation
        }

        public async Task DeleteProductAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid product ID.", nameof(id));

            var result = await _products.DeleteOneAsync(p => p.Id == id);
            if (result.DeletedCount == 0)
                throw new KeyNotFoundException($"No product found with ID: {id}");
        }

        public async Task ActivateProductAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid product ID.", nameof(id));

            var update = Builders<Product>.Update.Set(p => p.IsActive, true);
            var result = await _products.UpdateOneAsync(p => p.Id == id, update);
            if (result.ModifiedCount == 0)
                throw new KeyNotFoundException($"No product found with ID: {id}");
        }

        public async Task DeactivateProductAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid product ID.", nameof(id));

            var update = Builders<Product>.Update.Set(p => p.IsActive, false);
            var result = await _products.UpdateOneAsync(p => p.Id == id, update);
            if (result.ModifiedCount == 0)
                throw new KeyNotFoundException($"No product found with ID: {id}");
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _products.Find(p => true).ToListAsync(); // Retrieve all products
        }

        public async Task<List<Product>> GetActiveProductsAsync()
        {
            return await _products.Find(p => p.IsActive).ToListAsync(); // Retrieve active products
        }

        public async Task<List<Product>> GetInactiveProductsAsync()
        {
            return await _products.Find(p => !p.IsActive).ToListAsync(); // Retrieve inactive products
        }

        public async Task<Product?> GetVendorProductByIdAsync(string id, string productId)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid vendor ID.", nameof(id));
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("Invalid product ID.", nameof(productId));

            return await _products.Find(p => p.Id == productId && p.VendorId == id).FirstOrDefaultAsync();
        }
    }
}
