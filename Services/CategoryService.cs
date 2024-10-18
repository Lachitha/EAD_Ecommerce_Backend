using MongoDB.Driver;
using MongoDbConsoleApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDbConsoleApp.Services
{
    public class CategoryService
    {
        private readonly IMongoCollection<Category> _categories;

        public CategoryService(MongoDbService mongoDbService)
        {
            _categories = mongoDbService.GetCollection<Category>("Categories");

            // Ensure a unique index on the Name field
            CreateUniqueIndexOnName();
        }

        // Create a unique index on the Name field to enforce uniqueness
        private void CreateUniqueIndexOnName()
        {
            var indexKeys = Builders<Category>.IndexKeys.Ascending(c => c.Name);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<Category>(indexKeys, indexOptions);
            _categories.Indexes.CreateOne(indexModel);
        }

        // Create a new category
        public async Task CreateCategoryAsync(Category category)
        {
            try
            {
                await _categories.InsertOneAsync(category);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new Exception("A category with the same name already exists.");
            }
        }

        // Get all categories
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _categories.Find(c => true).ToListAsync();
        }

        // Get category by ID
        public async Task<Category?> GetCategoryByIdAsync(string id)
        {
            return await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        // Update an existing category
        public async Task UpdateCategoryAsync(string id, Category updatedCategory)
        {
            await _categories.ReplaceOneAsync(c => c.Id == id, updatedCategory);
        }

        // Delete a category by ID
        public async Task DeleteCategoryAsync(string id)
        {
            await _categories.DeleteOneAsync(c => c.Id == id);
        }
        public async Task<List<Category>> GetCategoriesByIdsAsync(List<string> ids)
        {
            return await _categories.Find(c => ids.Contains(c.Id)).ToListAsync();
        }
    }
}
