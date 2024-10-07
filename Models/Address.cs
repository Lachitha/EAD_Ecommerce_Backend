namespace MongoDbConsoleApp.Models
{
    public class Address
    {
        public string Street { get; set; } = string.Empty;  // Street address
        public string City { get; set; } = string.Empty;    // City
        public string State { get; set; } = string.Empty;   // State
        public string PostalCode { get; set; } = string.Empty; // Postal Code
        public string Country { get; set; } = string.Empty;  // Country
    }
}
