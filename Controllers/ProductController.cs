using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDbConsoleApp.Models;
using MongoDbConsoleApp.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MongoDbConsoleApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductController(ProductService productService)
        {
            _productService = productService;
        }

        [Authorize(Roles = "Vendor")]
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] Product product)
        {
            // Extract the VendorId from the JWT token
            var vendorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(vendorId))
            {
                return Unauthorized("Vendor ID not found in token.");
            }

            // Set the VendorId and ensure Quantity is provided
            product.VendorId = vendorId;

            if (product.Quantity <= 0) // Validate quantity
            {
                return BadRequest("Quantity must be greater than zero.");
            }

            // Automatically set the initial stock to be equal to the product's quantity
            product.Stock = product.Quantity;

            // Create the product in the database
            await _productService.CreateProductAsync(product);

            return Ok(new { message = "Product created successfully.", productId = product.Id });
        }

        [Authorize(Roles = "Vendor")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(string id, [FromBody] Product product)
        {
            var existingProduct = await _productService.GetProductByIdAsync(id);
            if (existingProduct == null)
            {
                return NotFound("Product not found.");
            }

            product.Id = id; // Ensure the product ID matches the one being updated
            await _productService.UpdateProductAsync(id, product);
            return Ok(new { message = "Product updated successfully." });
        }

        [Authorize(Roles = "Vendor")]
        [HttpPut("{id}/stock")]
        public async Task<IActionResult> UpdateStock(string id, [FromBody] int quantityChange)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            // Update the stock based on quantity change
            if (product.Stock + quantityChange < 0)
            {
                return BadRequest("Cannot reduce stock below zero.");
            }

            product.Stock += quantityChange;
            await _productService.UpdateProductStockAsync(id, product.Stock);

            // Check for low stock
            if (product.Stock < product.LowStockThreshold)
            {
                // Notify vendor (placeholder for notification logic)
                return Ok(new { message = "Stock updated successfully.", lowStockAlert = "Product is below the low stock threshold!" });
            }

            return Ok(new { message = "Stock updated successfully." });
        }

        [Authorize(Roles = "Vendor")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            var existingProduct = await _productService.GetProductByIdAsync(id);
            if (existingProduct == null)
            {
                return NotFound("Product not found.");
            }

            await _productService.DeleteProductAsync(id);
            return Ok(new { message = "Product deleted successfully." });
        }

        [Authorize(Roles = "Administrator")]
        [HttpPut("activate/{id}")]
        public async Task<IActionResult> ActivateProduct(string id)
        {
            await _productService.ActivateProductAsync(id);
            return Ok(new { message = "Product activated successfully." });
        }

        [Authorize(Roles = "Administrator")]
        [HttpPut("deactivate/{id}")]
        public async Task<IActionResult> DeactivateProduct(string id)
        {
            await _productService.DeactivateProductAsync(id);
            return Ok(new { message = "Product deactivated successfully." });
        }
    }
}
