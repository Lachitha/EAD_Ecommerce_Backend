using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDbConsoleApp.Models;
using MongoDbConsoleApp.Services;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MongoDbConsoleApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly CartService _cartService;

        public CartController(CartService cartService)
        {
            _cartService = cartService;
        }

        // Get cart by userId from JWT
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<ActionResult<Cart>> GetCart()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Adjust based on your JWT claim type
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var cart = await _cartService.GetCartByUserId(userId);
            if (cart == null)
            {
                return NotFound("Cart not found.");
            }

            return Ok(cart);
        }

        // Add product to cart
        [Authorize(Roles = "Customer")]
        [HttpPost("add")]
        public async Task<ActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Adjust based on your JWT claim type
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            if (request.quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero.");
            }

            try
            {
                await _cartService.AddToCart(userId, request.ProductId, request.quantity);
                return Ok(new { message = "Product added to cart." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Remove product from cart
        [Authorize(Roles = "Customer")]
        [HttpDelete("remove")]
        public async Task<ActionResult> RemoveFromCart([FromBody] RemoveFromCartRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Adjust based on your JWT claim type
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            try
            {
                await _cartService.RemoveFromCart(userId, request.ProductId);
                return Ok("Product removed from cart.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Update product quantity in cart
        [Authorize(Roles = "Customer")]
        [HttpPut("update")]
        public async Task<ActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Adjust based on your JWT claim type
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            if (request.NewQuantity <= 0)
            {
                return BadRequest("New quantity must be greater than zero.");
            }

            try
            {
                await _cartService.UpdateQuantity(userId, request.ProductId, request.NewQuantity);
                return Ok("Quantity updated.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    // Define request models for cleaner API
    public class AddToCartRequest
    {
        public string ProductId { get; set; }
        public int quantity { get; set; }
    }

    public class RemoveFromCartRequest
    {
        public string ProductId { get; set; }
    }

    public class UpdateQuantityRequest
    {
        public string ProductId { get; set; }
        public int NewQuantity { get; set; }
    }
}
