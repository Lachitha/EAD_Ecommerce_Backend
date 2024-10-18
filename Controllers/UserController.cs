using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDbConsoleApp.Models;
using MongoDbConsoleApp.Services;
using MongoDbConsoleApp.Helpers;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace MongoDbConsoleApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly JwtHelper _jwtHelper;

        public UserController(UserService userService, JwtHelper jwtHelper)
        {
            _userService = userService;
            _jwtHelper = jwtHelper;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            // Check if the request body is valid
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return BadRequest("Username, email, and password are required.");
            }

            // Validate the role of the new user being created
            if (!Role.IsValidRole(user.Role))
            {
                return BadRequest("Invalid role specified.");
            }

            // Customers can register themselves
            if (user.Role == Role.Customer)
            {
                // Check if the username already exists
                if (await _userService.FindByUsernameAsync(user.Username) != null)
                {
                    return BadRequest("Username already exists.");
                }

                // Check if the email already exists
                if (await _userService.FindByEmailAsync(user.Email) != null)
                {
                    return BadRequest("Email already exists.");
                }

                // Hash the user's password and create the user
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                await _userService.CreateUserAsync(user);

                return Ok(new { message = "Customer registered successfully.", userId = user.Id });
            }

            // For roles other than Customer (e.g., Vendor or Administrator)
            if (user.Role == Role.Vendor || user.Role == Role.Administrator)
            {
                // Ensure the logged-in user is an Administrator
                if (!User.IsInRole(Role.Administrator))
                {
                    return Unauthorized("Only administrators can create vendors or new administrators.");
                }

                // Check if the username already exists
                if (await _userService.FindByUsernameAsync(user.Username) != null)
                {
                    return BadRequest("Username already exists.");
                }

                // Check if the email already exists
                if (await _userService.FindByEmailAsync(user.Email) != null)
                {
                    return BadRequest("Email already exists.");
                }

                // Hash the user's password and create the user
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                await _userService.CreateUserAsync(user);

                return Ok(new { message = $"{user.Role} registered successfully.", userId = user.Id });
            }

            return BadRequest("Invalid user role.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return BadRequest("Email and password are required.");
            }

            var existingUser = await _userService.FindByEmailAsync(user.Email);
            if (existingUser == null || !BCrypt.Net.BCrypt.Verify(user.PasswordHash, existingUser.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }

            var token = _jwtHelper.GenerateToken(existingUser.Id, existingUser.Username, existingUser.Role);
            return Ok(new { Token = token, Role = existingUser.Role });
        }

        [Authorize]
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] User updatedUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Username and email are required.");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid token.");
            }

            var existingUser = await _userService.FindByIdAsync(userId);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }

            existingUser.Username = updatedUser.Username;
            existingUser.Email = updatedUser.Email;

            if (!string.IsNullOrWhiteSpace(updatedUser.PasswordHash))
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updatedUser.PasswordHash);
            }

            await _userService.UpdateUserAsync(existingUser);
            return Ok(new { message = "User details updated successfully.", user = existingUser });
        }

        [Authorize]
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid token.");
            }

            var existingUser = await _userService.FindByIdAsync(userId);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }

            await _userService.DeleteUserAsync(userId);
            return Ok(new { message = "User deleted successfully." });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetUserDetails()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid token.");
            }

            var existingUser = await _userService.FindByIdAsync(userId);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }

            var userDetails = new
            {
                existingUser.FirstName,
                existingUser.LastName,
                existingUser.Username,
                existingUser.Role,
                existingUser.Email,
                existingUser.Address,
                DateOfBirth = existingUser.DateOfBirth.HasValue ? existingUser.DateOfBirth.Value.ToString("yyyy-MM-dd") : null
            };

            return Ok(userDetails);
        }

        [Authorize(Roles = Role.Customer)]
        [HttpPost("{vendorId}/rate")]
        public async Task<IActionResult> RateVendor(string vendorId, [FromBody] VendorRating rating)
        {
            if (rating.Rating < 1 || rating.Rating > 5)
            {
                return BadRequest("Rating should be between 1 and 5.");
            }

            var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            rating.CustomerId = customerId;

            await _userService.AddVendorRatingAsync(vendorId, rating);
            return Ok(new { message = "Rating and comment added successfully." });
        }

        [HttpGet("vendors")]
        public async Task<IActionResult> GetVendors()
        {
            var vendors = await _userService.GetVendorsAsync();
            return Ok(vendors.Select(v => new
            {
                v.VendorName,
                v.VendorDescription,
                AverageRating = v.AverageRating
            }));
        }

        [Authorize(Roles = Role.Customer)]
        [HttpPut("{vendorId}/edit-comment")]
        public async Task<IActionResult> EditComment(string vendorId, [FromBody] string newComment)
        {
            var customerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Logic to ensure the customer can edit only their own comment
            await _userService.EditVendorCommentAsync(vendorId, customerId, newComment);
            return Ok(new { message = "Comment updated successfully." });
        }

        // New: View all users (Admin only)
        [Authorize(Roles = Role.Administrator)]
        [HttpGet("all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);  // Returns all details of all users
        }

        // New: View only customers (Admin only)
        [Authorize(Roles = Role.Administrator)]
        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _userService.GetUsersByRoleAsync(Role.Customer);
            return Ok(customers);  // Returns all details of customers
        }
    }
}
