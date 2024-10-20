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

        public ProductService(MongoDbService database)
        {
            _products = database.GetCollection<Product>("Products");

        }

        public async Task CreateProductAsync(Product product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product), "Product cannot be null.");
            }

            try
            {
                await _products.InsertOneAsync(product);
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while creating the product.", ex);
            }
        }

        public async Task<Product?> GetProductByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("ID cannot be null or empty.", nameof(id));
            }

            try
            {
                return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while fetching the product.", ex);
            }
        }

        public async Task<List<Product>> GetAllProductsAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                return await _products.Find(p => true)
                    .Skip((pageNumber - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while fetching products.", ex);
            }
        }

        public async Task<List<Product>> GetActiveProductsAsync()
        {
            try
            {
                return await _products.Find(p => p.IsActive).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while fetching active products.", ex);
            }
        }

        public async Task<List<Product>> GetInactiveProductsAsync()
        {
            try
            {
                return await _products.Find(p => !p.IsActive).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while fetching inactive products.", ex);
            }
        }

        public async Task<Product?> GetVendorProductByIdAsync(string productId, string vendorId)
        {
            if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(vendorId))
            {
                throw new ArgumentException("Product ID and Vendor ID cannot be null or empty.");
            }

            try
            {
                return await _products.Find(p => p.Id == productId && p.VendorId == vendorId).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while fetching the vendor's product.", ex);
            }
        }

        public async Task<List<Product>> GetVendorProductsAsync(string vendorId)
        {
            if (string.IsNullOrEmpty(vendorId))
            {
                throw new ArgumentException("Vendor ID cannot be null or empty.", nameof(vendorId));
            }

            try
            {
                return await _products.Find(p => p.VendorId == vendorId).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while fetching vendor products.", ex);
            }
        }

        public async Task UpdateProductAsync(string id, Product product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product), "Product cannot be null.");
            }

            try
            {
                await _products.ReplaceOneAsync(p => p.Id == id, product);
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while updating the product.", ex);
            }
        }

        public async Task DeleteProductAsync(string id)
        {
            try
            {
                await _products.DeleteOneAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while deleting the product.", ex);
            }
        }

        public async Task ActivateProductAsync(string id)
        {
            try
            {
                var update = Builders<Product>.Update.Set(p => p.IsActive, true);
                await _products.UpdateOneAsync(p => p.Id == id, update);
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while activating the product.", ex);
            }
        }

        public async Task DeactivateProductAsync(string id)
        {
            try
            {
                var update = Builders<Product>.Update.Set(p => p.IsActive, false);
                await _products.UpdateOneAsync(p => p.Id == id, update);
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while deactivating the product.", ex);
            }
        }

        public async Task<List<Product>> GetVendorLowStockProductsAsync(string vendorId)
        {
            if (string.IsNullOrEmpty(vendorId))
            {
                throw new ArgumentException("Vendor ID cannot be null or empty.", nameof(vendorId));
            }

            try
            {
                return await _products.Find(p => p.VendorId == vendorId && p.Stock < p.LowStockThreshold).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while fetching low-stock vendor products.", ex);
            }
        }

        // New method to get all low-stock products for the administrator
        public async Task<List<Product>> GetAllLowStockProductsAsync()
        {
            try
            {
                return await _products.Find(p => p.Stock < p.LowStockThreshold).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while fetching all low-stock products.", ex);
            }
        }

        // New method to get products by a list of IDs
        public async Task<List<Product>> GetProductsByIdsAsync(List<string> productIds)
        {
            if (productIds == null || productIds.Count == 0)
            {
                throw new ArgumentException("Product IDs cannot be null or empty.", nameof(productIds));
            }

            try
            {
                return await _products.Find(p => productIds.Contains(p.Id)).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while fetching products by IDs.", ex);
            }
        }




    }
}
