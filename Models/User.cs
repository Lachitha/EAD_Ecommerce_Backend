using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDbConsoleApp.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }  // Nullable Id for MongoDB

        public string Username { get; set; } = string.Empty;  // Default value to prevent nullability
        public string PasswordHash { get; set; } = string.Empty;  // Default value to prevent nullability
        public string Role { get; set; } = string.Empty;  // Default value to prevent nullability

        // Additional properties
        public string Email { get; set; } = string.Empty;  // User's email address
        public string FirstName { get; set; } = string.Empty;  // User's first name
        public string LastName { get; set; } = string.Empty;  // User's last name
        public DateTime DateOfBirth { get; set; }  // User's date of birth

        // Address property
        public Address Address { get; set; } = new Address();  // User's address
    }
}
