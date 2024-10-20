using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDbConsoleApp.Models;
using MongoDbConsoleApp.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MongoDbConsoleApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly CartService _cartService;
        private readonly ProductService _productService; // Added to fetch product details

        public CartController(CartService cartService, ProductService productService)
        {
            _cartService = cartService;
            _productService = productService;
        }

        // Get cart by userId from JWT
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<ActionResult<CartDetailsDto>> GetCart()
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

            // Create a list to hold product details
            var cartDetails = new CartDetailsDto
            {
                UserId = cart.UserId,
                Items = new List<CartItemDetailsDto>(),
                TotalAmount = 0
            };

            // Fetch product details for each item in the cart
            foreach (var cartItem in cart.Items)
            {
                var product = await _productService.GetProductByIdAsync(cartItem.ProductId);
                if (product != null)
                {
                    var itemTotal = cartItem.Quantity * cartItem.Price;
                    cartDetails.Items.Add(new CartItemDetailsDto
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Price,
                        Total = itemTotal,
                        // Calculate total
                        ProductDetails = product // Include product details
                    });
                    cartDetails.TotalAmount += itemTotal;
                }
            }

            return Ok(cartDetails);
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

            if (request.Quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero.");
            }

            try
            {
                await _cartService.AddToCart(userId, request.ProductId, request.Quantity);
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
        public int Quantity { get; set; }
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

    // Define DTOs for cart details
    public class CartDetailsDto
    {
        public string UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<CartItemDetailsDto> Items { get; set; }
    }

    public class CartItemDetailsDto
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
        public Product ProductDetails { get; set; } // Include full product details
    }
}
