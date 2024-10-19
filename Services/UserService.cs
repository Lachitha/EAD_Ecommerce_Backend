using MongoDB.Driver;
using MongoDbConsoleApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

        // Create a new user
        public async Task CreateUserAsync(User user)
        {
            await _users.InsertOneAsync(user);
        }

        // Find user by username
        public async Task<User?> FindByUsernameAsync(string username)
        {
            return await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        }

        // Find user by email
        public async Task<User?> FindByEmailAsync(string email)
        {
            return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

        // Find user by id
        public async Task<User?> FindByIdAsync(string id)
        {
            return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        // Delete user by id
        public async Task DeleteUserAsync(string id)
        {
            await _users.DeleteOneAsync(u => u.Id == id);
        }

        // Update an existing user
        public async Task UpdateUserAsync(User user)
        {
            if (string.IsNullOrEmpty(user.Id))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(user.Id));
            }

            await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
        }

        // Add vendor rating
        public async Task AddVendorRatingAsync(string vendorId, VendorRating rating)
        {
            var update = Builders<User>.Update.Push(u => u.Ratings, rating);
            await _users.UpdateOneAsync(u => u.Id == vendorId, update);
        }

        // Get all vendors
        public async Task<List<User>> GetVendorsAsync()
        {
            return await _users.Find(u => u.Role == Role.Vendor).ToListAsync();
        }

        // Edit vendor comment by customer
        public async Task EditVendorCommentAsync(string vendorId, string customerId, string newComment)
        {
            var update = Builders<User>.Update
                .Set(u => u.Ratings[-1].Comment, newComment); // Update the specific comment by the customer
            await _users.UpdateOneAsync(u => u.Id == vendorId && u.Ratings.Any(r => r.CustomerId == customerId), update);
        }

        // New method: Get all users
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _users.Find(u => true).ToListAsync();  // Retrieves all users from the collection
        }

        // New method: Get users by role
        public async Task<List<User>> GetUsersByRoleAsync(string role)
        {
            return await _users.Find(u => u.Role == role).ToListAsync();  // Retrieves users based on their role
        }

        // New method: Get all inactive users
        public async Task<List<User>> GetInactiveUsersAsync()
        {
            return await _users.Find(u => !u.IsActive).ToListAsync();  // Retrieves all inactive users
        }

        // New method: Reactivate a user
        public async Task ReactivateUserAsync(string userId)
        {
            var user = await FindByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found.", nameof(userId));
            }

            user.IsActive = true;  // Set account status to active
            await UpdateUserAsync(user);  // Save the changes
        }

        public async Task<List<User>> GetUsersByRolesAsync(params string[] roles)
        {
            return await _users.Find(u => roles.Contains(u.Role)).ToListAsync();
        }
    }
}
