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
            var vendorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(vendorId))
            {
                return Unauthorized("Vendor ID not found in token.");
            }

            product.VendorId = vendorId;

            if (product.Quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero.");
            }

            product.Stock = product.Quantity;

            await _productService.CreateProductAsync(product);

            return Ok(new { message = "Product created successfully.", productId = product.Id });
        }

        [Authorize(Roles = "Vendor")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(string id, [FromBody] Product product)
        {
            if (product == null)
            {
                return BadRequest("Product cannot be null.");
            }

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
        [HttpPut("{id}/quantity")]
        public async Task<IActionResult> AddStock(string id, [FromBody] int additionalQuantity)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            // Validate additional quantity
            if (additionalQuantity <= 0)
            {
                return BadRequest("Quantity to add must be greater than zero.");
            }

            // Update stock by adding the new quantity
            product.Stock += additionalQuantity;
            product.Quantity += additionalQuantity;

            await _productService.UpdateProductAsync(id, product);

            // Check for low stock alert
            if (product.Stock < product.LowStockThreshold)
            {
                return Ok(new { message = "Quantity updated successfully.", lowStockAlert = "Product is below the low stock threshold!" });
            }

            return Ok(new { message = "Quantity updated successfully." });
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
        [Authorize(Roles = "Administrator,Customer")]
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products); // Return the list of products
        }
    }
}
