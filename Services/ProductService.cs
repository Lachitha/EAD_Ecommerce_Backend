// ProductService.cs
using MongoDB.Driver;
using MongoDbConsoleApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // Create a new product
        public async Task CreateProductAsync(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product), "Product cannot be null.");

            product.Stock = product.Quantity; // Set initial stock based on quantity

            await _products.InsertOneAsync(product);

            // Check and notify if low stock after creation
            await CheckAndNotifyLowStockAsync(product);
        }

        // Retrieve a product by ID
        public async Task<Product?> GetProductByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid product ID.", nameof(id));

            return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        // Update an existing product
        public async Task UpdateProductAsync(string id, Product product)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid product ID.", nameof(id));

            if (product == null)
                throw new ArgumentNullException(nameof(product), "Product cannot be null.");

            // Ensure the product ID matches the one being updated
            product.Id = id;

            var existingProduct = await GetProductByIdAsync(id);
            if (existingProduct == null)
                throw new KeyNotFoundException($"No product found with ID: {id}");

            // Update the stock based on the new quantity
            product.Stock = existingProduct.Stock - (existingProduct.Quantity - product.Quantity);
            product.Quantity = product.Quantity; // Update quantity

            var result = await _products.ReplaceOneAsync(p => p.Id == id, product);
            if (result.ModifiedCount == 0)
                throw new KeyNotFoundException($"No product found with ID: {id}");

            // Check and notify if low stock after update
            await CheckAndNotifyLowStockAsync(product);
        }

        // Delete a product by ID
        public async Task DeleteProductAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid product ID.", nameof(id));

            var result = await _products.DeleteOneAsync(p => p.Id == id);
            if (result.DeletedCount == 0)
                throw new KeyNotFoundException($"No product found with ID: {id}");
        }

        // Activate a product
        public async Task ActivateProductAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid product ID.", nameof(id));

            var update = Builders<Product>.Update.Set(p => p.IsActive, true);
            var result = await _products.UpdateOneAsync(p => p.Id == id, update);
            if (result.ModifiedCount == 0)
                throw new KeyNotFoundException($"No product found with ID: {id}");
        }

        // Deactivate a product
        public async Task DeactivateProductAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid product ID.", nameof(id));

            var update = Builders<Product>.Update.Set(p => p.IsActive, false);
            var result = await _products.UpdateOneAsync(p => p.Id == id, update);
            if (result.ModifiedCount == 0)
                throw new KeyNotFoundException($"No product found with ID: {id}");
        }

        // Get all products
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _products.Find(p => true).ToListAsync(); // Retrieve all products
        }

        // Get active products
        public async Task<List<Product>> GetActiveProductsAsync()
        {
            return await _products.Find(p => p.IsActive).ToListAsync(); // Retrieve active products
        }

        // Get inactive products
        public async Task<List<Product>> GetInactiveProductsAsync()
        {
            return await _products.Find(p => !p.IsActive).ToListAsync(); // Retrieve inactive products
        }

        // Get a specific product for a vendor
        public async Task<Product?> GetVendorProductByIdAsync(string vendorId, string productId)
        {
            if (string.IsNullOrEmpty(vendorId))
                throw new ArgumentException("Invalid vendor ID.", nameof(vendorId));
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("Invalid product ID.", nameof(productId));

            return await _products.Find(p => p.Id == productId && p.VendorId == vendorId).FirstOrDefaultAsync();
        }

        // Check and notify if product stock is low
        private async Task CheckAndNotifyLowStockAsync(Product product)
        {
            if (product.Stock < product.LowStockThreshold)
            {
                await NotifyVendorLowStockAsync(product);
            }
        }

        // Notify vendor about low stock
        private async Task NotifyVendorLowStockAsync(Product product)
        {
            Console.WriteLine($"Low stock alert: Product '{product.Name}' is below the low stock threshold. Current stock: {product.Stock}");
            await Task.CompletedTask; // Simulate async operation
        }

        // Add a new category
        public async Task AddCategoryAsync(string productId, Product.ProductCategoryDetails category)
        {
            var update = Builders<Product>.Update.Push(p => p.Categories, category);
            await _products.UpdateOneAsync(p => p.Id == productId, update);
        }

        // Update an existing category
        public async Task UpdateCategoryAsync(string productId, string categoryId, Product.ProductCategoryDetails updatedCategory)
        {
            var product = await GetProductByIdAsync(productId);
            if (product == null) throw new KeyNotFoundException($"No product found with ID: {productId}");

            // Find the category to update
            var category = product.Categories.FirstOrDefault(c => c.Id == categoryId);
            if (category == null) throw new KeyNotFoundException($"No category found with ID: {categoryId}");

            // Update the category
            category.Name = updatedCategory.Name;
            category.Description = updatedCategory.Description;

            // Save the updated product back to the database
            await _products.ReplaceOneAsync(p => p.Id == productId, product);
        }

        // Delete a category
        public async Task DeleteCategoryAsync(string productId, string categoryId)
        {
            var product = await GetProductByIdAsync(productId);
            if (product == null) throw new KeyNotFoundException($"No product found with ID: {productId}");

            // Filter to remove the category by ID
            var update = Builders<Product>.Update.PullFilter(p => p.Categories, c => c.Id == categoryId);
            await _products.UpdateOneAsync(p => p.Id == productId, update);
        }

        // View all categories for a product
        public async Task<List<Product.ProductCategoryDetails>> GetCategoriesAsync(string productId)
        {
            var product = await GetProductByIdAsync(productId);
            return product?.Categories ?? new List<Product.ProductCategoryDetails>();
        }
    }
}
