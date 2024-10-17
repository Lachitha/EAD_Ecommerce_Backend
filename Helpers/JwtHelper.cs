using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace MongoDbConsoleApp.Helpers
{
    public class JwtHelper
    {
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;

        public JwtHelper(string key, string issuer, string audience)
        {
            // Validate key length for HS256
            if (string.IsNullOrEmpty(key) || Encoding.UTF8.GetBytes(key).Length < 32)
            {
                throw new ArgumentException("JWT key must be at least 32 bytes for HS256.");
            }

            _key = key;
            _issuer = issuer;
            _audience = audience;
        }

        public string GenerateToken(string userId, string username, string role)
        {
            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var signingCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, role)
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Method to extract the UserId from the JWT token
        public string GetUserIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken == null)
                throw new SecurityTokenException("Invalid token");

            var userIdClaim = jwtToken?.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier);
            return userIdClaim?.Value;
        }
    }
}
