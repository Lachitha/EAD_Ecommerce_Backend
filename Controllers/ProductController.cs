using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDbConsoleApp.Models;
using MongoDbConsoleApp.Services;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using MongoDB.Bson;

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
        public async Task<IActionResult> CreateProduct([FromBody] Product request)
        {
            var vendorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(vendorId))
            {
                return Unauthorized("Vendor ID not found in token.");
            }

            // Create a new product from the request
            var product = new Product
            {
                VendorId = vendorId,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Quantity = request.Quantity,
                Stock = request.Quantity,
                ImageBase64 = request.ImageBase64,
                Categories = request.Categories // Directly assign the list of categories from the request
            };

            if (product.Quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero.");
            }

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
        [HttpGet("all")]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _productService.GetAllProductsAsync();

            if (User.IsInRole("Customer"))
            {
                // Filter for customers to include only activated products
                products = products.Where(p => p.IsActive).ToList();
            }

            var productResponses = products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Quantity,
                p.Stock,
                p.IsActive,
                p.LowStockThreshold,
                p.VendorId,
                p.Categories,
                Image = ConvertImageFromBase64(p.ImageBase64) // Convert Base64 image to image format
            }).ToList();

            return Ok(productResponses); // Return the modified list of products
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveProducts()
        {
            var products = await _productService.GetAllProductsAsync();

            var activeProducts = products.Where(p => p.IsActive).Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Quantity,
                p.Stock,
                p.IsActive,
                p.LowStockThreshold,
                p.VendorId,
                p.Categories,
                Image = ConvertImageFromBase64(p.ImageBase64) // Convert Base64 image to image format
            }).ToList();

            return Ok(activeProducts); // Return the list of active products
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("inactive")]
        public async Task<IActionResult> GetInactiveProducts()
        {
            var products = await _productService.GetAllProductsAsync();

            var inactiveProducts = products.Where(p => !p.IsActive).Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.Quantity,
                p.Stock,
                p.IsActive,
                p.LowStockThreshold,
                p.VendorId,
                p.Categories,
                Image = ConvertImageFromBase64(p.ImageBase64) // Convert Base64 image to image format
            }).ToList();

            return Ok(inactiveProducts); // Return the list of inactive products
        }

        private string ConvertImageFromBase64(string? base64Image)
        {
            if (string.IsNullOrEmpty(base64Image))
            {
                return string.Empty; // Return an empty string if no image is provided
            }

            // Convert base64 string to image file URL (data URL)
            return $"data:image/jpeg;base64,{base64Image}"; // Use "image/png" if the image is PNG
        }


        [Authorize(Roles = "Administrator,Customer")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(string id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            // Convert the image to base64 if it exists
            if (!string.IsNullOrEmpty(product.ImageBase64))
            {
                product.ImageBase64 = ConvertToBase64(product.ImageBase64);
            }

            return Ok(product);
        }

        // Convert image to base64 format (if needed)
        private string ConvertToBase64(string imageBase64)
        {
            // Assuming the image is already in base64, this method could be used for additional processing
            return imageBase64;
        }


        [Authorize(Roles = "Vendor")]
        [HttpGet("vendor/{id}")]
        public async Task<IActionResult> GetVendorProductById(string id)
        {
            var vendorId = User.FindFirst("id")?.Value; // Assuming the vendor ID is stored in the JWT token

            if (string.IsNullOrEmpty(vendorId))
            {
                return Unauthorized("Vendor ID is not available in the token.");
            }

            var product = await _productService.GetVendorProductByIdAsync(vendorId, id);
            if (product == null)
            {
                return NotFound("Product not found or does not belong to the vendor.");
            }

            return Ok(product);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost("{id}/category")]
        public async Task<IActionResult> AddCategory(string id, [FromBody] Product.ProductCategoryDetails category)
        {
            if (category == null)
            {
                return BadRequest("Category cannot be null.");
            }

            // Ensure the category has an ID
            category.Id = ObjectId.GenerateNewId().ToString(); // Generate a new ID for the category

            await _productService.AddCategoryAsync(id, category);
            return Ok(new { message = "Category added successfully." });
        }

        // Update an existing category
        [Authorize(Roles = "Administrator")]
        [HttpPut("{id}/category/{categoryId}")]
        public async Task<IActionResult> UpdateCategory(string id, string categoryId, [FromBody] Product.ProductCategoryDetails updatedCategory)
        {
            if (updatedCategory == null)
            {
                return BadRequest("Updated category cannot be null.");
            }

            await _productService.UpdateCategoryAsync(id, categoryId, updatedCategory);
            return Ok(new { message = "Category updated successfully." });
        }

        // Delete a category
        [Authorize(Roles = "Administrator")]
        [HttpDelete("{id}/category/{categoryId}")]
        public async Task<IActionResult> DeleteCategory(string id, string categoryId)
        {
            await _productService.DeleteCategoryAsync(id, categoryId);
            return Ok(new { message = "Category deleted successfully." });
        }

        // Get all categories for a product
        [HttpGet("{id}/category")]
        public async Task<IActionResult> GetCategories(string id)
        {
            var categories = await _productService.GetCategoriesAsync(id);
            return Ok(categories);
        }

    }
}
