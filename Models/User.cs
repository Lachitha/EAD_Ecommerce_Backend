using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace MongoDbConsoleApp.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }  // Nullable Id for MongoDB
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        public string Username { get; set; } = string.Empty;  // Default value to prevent nullability
        public string PasswordHash { get; set; } = string.Empty;  // Default value to prevent nullability
        public string Role { get; set; } = string.Empty;  // Default value to prevent nullability

        // Additional properties
        public string Email { get; set; } = string.Empty;  // User's email address
        public string FirstName { get; set; } = string.Empty;  // User's first name
        public string LastName { get; set; } = string.Empty;  // User's last name
                                                              // User's date of birth
        public DateTime? DateOfBirth { get; set; }

        // Address property
        public Address Address { get; set; } = new Address();  // User's address

        // Vendor-specific properties
        public string? VendorName { get; set; }  // Vendor's name (if role is Vendor)
        public string? VendorDescription { get; set; }  // Vendor's description (if role is Vendor)

        // List of customer ratings and comments for the vendor
        public List<VendorRating> Ratings { get; set; } = new List<VendorRating>();

        [BsonIgnore]
        public double AverageRating => Ratings.Count > 0 ? Ratings.Average(r => r.Rating) : 0;  // Calculate average rating
    }

    public class VendorRating
    {
        public string CustomerId { get; set; } = string.Empty;  // ID of the customer who rated the vendor
        public int Rating { get; set; }  // Rating (1-5)
        public string Comment { get; set; } = string.Empty;  // Customer's comment
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // Timestamp for the rating
    }

    public class PasswordResetRequest
    {

        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
