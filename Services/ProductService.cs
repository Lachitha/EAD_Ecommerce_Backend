using MongoDB.Driver;
using MongoDbConsoleApp.Models;
using System.Threading.Tasks;

namespace MongoDbConsoleApp.Services
{
    public class ProductService
    {
        private readonly IMongoCollection<Product> _products;

        public ProductService(MongoDbService mongoDbService)
        {
            // Initialize the MongoDB collection for products
            _products = mongoDbService.GetCollection<Product>("Products"); // Ensure this matches your collection name
        }

        public async Task CreateProductAsync(Product product)
        {
            // Optionally, validate the product before insertion
            if (product == null)
                throw new ArgumentNullException(nameof(product), "Product cannot be null.");

            await _products.InsertOneAsync(product);
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

            // Ensure the product ID matches the one being updated
            product.Id = id;
            var result = await _products.ReplaceOneAsync(p => p.Id == id, product);
            if (result.ModifiedCount == 0)
                throw new KeyNotFoundException($"No product found with ID: {id}");
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
    }
}
