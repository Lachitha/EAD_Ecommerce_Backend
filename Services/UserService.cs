using MongoDB.Driver;
using MongoDbConsoleApp.Models;
using System.Threading.Tasks;

namespace MongoDbConsoleApp.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(MongoDbService mongoDbService)
        {
            _users = mongoDbService.GetCollection<User>("User");
        }

        public async Task CreateUserAsync(User user)
        {

            await _users.InsertOneAsync(user);
        }

        public async Task<User?> FindByUsernameAsync(string username)
        {
            return await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        }

        public async Task<User?> FindByEmailAsync(string email)
        {
            return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

        public async Task<User?> FindByIdAsync(string id)
        {
            return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task DeleteUserAsync(string id)
        {
            await _users.DeleteOneAsync(u => u.Id == id);
        }

        public async Task UpdateUserAsync(User user)
        {
            if (string.IsNullOrEmpty(user.Id))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(user.Id));
            }

            await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
        }

        public async Task AddVendorRatingAsync(string vendorId, VendorRating rating)
        {
            var update = Builders<User>.Update.Push(u => u.Ratings, rating);
            await _users.UpdateOneAsync(u => u.Id == vendorId, update);
        }

        public async Task<List<User>> GetVendorsAsync()
        {
            return await _users.Find(u => u.Role == Role.Vendor).ToListAsync();
        }

        public async Task EditVendorCommentAsync(string vendorId, string customerId, string newComment)
        {
            var update = Builders<User>.Update
                .Set(u => u.Ratings[-1].Comment, newComment); // Update the specific comment by the customer
            await _users.UpdateOneAsync(u => u.Id == vendorId && u.Ratings.Any(r => r.CustomerId == customerId), update);
        }
    }
}
