using MongoDB.Driver;
using MongoDbConsoleApp.Models;
using System.Threading.Tasks;

namespace MongoDbConsoleApp.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(MongoDbService mongoDbService) // Inject MongoDbService
        {
            // Get the "TestCollection" from the MongoDbService
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

        public async Task<bool> AuthenticateUserAsync(string email, string passwordHash)
        {
            var user = await _users.Find(u => u.Email == email && u.PasswordHash == passwordHash).FirstOrDefaultAsync();
            return user != null;
        }

        public async Task DeleteUserAsync(string id)
        {
            await _users.DeleteOneAsync(u => u.Id == id);
        }

        public async Task UpdateUserAsync(User user)
        {
            // Ensure that the user ID is set for the update operation
            if (string.IsNullOrEmpty(user.Id))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(user.Id));
            }

            // Replace the user in the collection
            await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
        }
    }
}
