using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDbConsoleApp.Models;
using MongoDbConsoleApp.Services;
using MongoDbConsoleApp.Helpers;
using System.Threading.Tasks;
using System.Security.Claims;

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

        // Register new user
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return BadRequest("Username, email, and password are required.");
            }

            if (!Role.IsValidRole(user.Role))
            {
                return BadRequest("Invalid role specified.");
            }

            if (await _userService.FindByUsernameAsync(user.Username) != null)
            {
                return BadRequest("Username already exists.");
            }

            if (await _userService.FindByEmailAsync(user.Email) != null)
            {
                return BadRequest("Email already exists.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();

            await _userService.CreateUserAsync(user);

            return Ok(new { message = "User registered successfully.", userId = user.Id });
        }

        // User login
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
            return Ok(new { Token = token });
        }

        // Update user details using JWT token
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

            if (!string.IsNullOrWhiteSpace(updatedUser.PasswordHash) &&
                !BCrypt.Net.BCrypt.Verify(updatedUser.PasswordHash, existingUser.PasswordHash))
            {
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updatedUser.PasswordHash);
            }

            await _userService.UpdateUserAsync(existingUser);
            return Ok(new { message = "User details updated successfully." });
        }

        // Delete user using JWT token
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
    }
}
