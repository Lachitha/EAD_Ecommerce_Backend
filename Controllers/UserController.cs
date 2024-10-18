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

                // At this point, IsActive is already set to true by default
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

                // At this point, IsActive is already set to true by default
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
            if (existingUser == null || !existingUser.IsActive || !BCrypt.Net.BCrypt.Verify(user.PasswordHash, existingUser.PasswordHash))
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

            // Check for unique username and email
            if (!string.Equals(existingUser.Username, updatedUser.Username, StringComparison.OrdinalIgnoreCase))
            {
                if (await _userService.FindByUsernameAsync(updatedUser.Username) != null)
                {
                    return BadRequest("Username already exists.");
                }
                existingUser.Username = updatedUser.Username; // Update only if unique
            }

            if (!string.Equals(existingUser.Email, updatedUser.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (await _userService.FindByEmailAsync(updatedUser.Email) != null)
                {
                    return BadRequest("Email already exists.");
                }
                existingUser.Email = updatedUser.Email; // Update only if unique
            }

            // Update other user details
            existingUser.FirstName = updatedUser.FirstName;
            existingUser.LastName = updatedUser.LastName;

            // Preserve the existing role (do not allow role changes)
            // existingUser.Role remains unchanged

            // Preserve the existing password if a new one is not provided
            if (!string.IsNullOrWhiteSpace(updatedUser.PasswordHash))
            {
                // Only update the password if a new one is provided
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updatedUser.PasswordHash);
            }

            // Update address
            if (updatedUser.Address != null)
            {
                existingUser.Address.Street = updatedUser.Address.Street;
                existingUser.Address.City = updatedUser.Address.City;
                existingUser.Address.State = updatedUser.Address.State;
                existingUser.Address.PostalCode = updatedUser.Address.PostalCode;
                existingUser.Address.Country = updatedUser.Address.Country;
            }

            // Update date of birth if provided
            if (updatedUser.DateOfBirth != default(DateTime))
            {
                existingUser.DateOfBirth = updatedUser.DateOfBirth;
            }

            // Save updated user details
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

        [Authorize(Roles = Role.Customer)]
        [HttpPost("deactivate")]
        public async Task<IActionResult> DeactivateAccount()
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

            // Deactivate the account
            existingUser.IsActive = false; // Set account status to inactive

            await _userService.UpdateUserAsync(existingUser);
            return Ok(new { message = "Account deactivated successfully." });
        }

    }
}
