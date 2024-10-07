using System.Collections.Generic;

namespace MongoDbConsoleApp.Models
{
    public static class Role
    {
        public const string Administrator = "Administrator";
        public const string Vendor = "Vendor";
        public const string CSR = "CSR";
        public const string Customer = "Customer";

        private static readonly List<string> ValidRoles = new List<string>
        {
            Administrator,
            Vendor,
            CSR,
            Customer
        };

        public static bool IsValidRole(string role)
        {
            return ValidRoles.Contains(role);
        }

        public static IEnumerable<string> GetAllRoles() => ValidRoles;
    }
}
