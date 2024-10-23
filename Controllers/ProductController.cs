using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDbConsoleApp.Models;
using MongoDbConsoleApp.Services;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MongoDbConsoleApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly UserService _userService;
        public ProductController(ProductService productService, CategoryService categoryService, UserService userService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _userService = userService;
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

            if (request.Quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero.");
            }
            if (request.CategoryIds == null || !request.CategoryIds.Any())
            {
                return BadRequest("CategoryIds cannot be empty.");
            }

            // Validate category IDs
            var existingCategoryIds = (await _categoryService.GetAllCategoriesAsync()).Select(c => c.Id).ToList();
            if (request.CategoryIds.Any(c => !existingCategoryIds.Contains(c)))
            {
                return BadRequest("One or more category IDs are invalid.");
            }

            // Create a new product
            var product = new Product
            {
                VendorId = vendorId,
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Quantity = request.Quantity,
                Stock = request.Quantity,
                //LowStockThreshold = request.LowStockThreshold,
                ImageBase64 = request.ImageBase64,
                CategoryIds = request.CategoryIds
            };

            await _productService.CreateProductAsync(product);
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, new { message = "Product created successfully.", productId = product.Id });
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

            // Prepare the product response with necessary details
            var categories = await _categoryService.GetCategoriesByIdsAsync(product.CategoryIds);
            var productResponse = new
            {
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Quantity,
                product.Stock,
                product.IsActive,
                product.LowStockThreshold,
                product.VendorId,
                Categories = categories,
                Image = ConvertImageFromBase64(product.ImageBase64) // Convert image from Base64
            };

            return Ok(productResponse);
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

            product.Id = id;
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

            if (additionalQuantity <= 0)
            {
                return BadRequest("Quantity to add must be greater than zero.");
            }

            product.Stock += additionalQuantity;
            product.Quantity += additionalQuantity;

            await _productService.UpdateProductAsync(id, product);

            var message = "Quantity updated successfully.";
            if (product.Stock < product.LowStockThreshold)
            {
                message += " Product is below the low stock threshold!";
            }

            return Ok(new { message });
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
                products = products.Where(p => p.IsActive).ToList();
            }

            var productResponses = await GetProductResponses(products);
            return Ok(productResponses);
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveProducts()
        {
            var products = await _productService.GetActiveProductsAsync();
            var activeProducts = await GetProductResponses(products);
            return Ok(activeProducts);
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("inactive")]
        public async Task<IActionResult> GetInactiveProducts()
        {
            var products = await _productService.GetInactiveProductsAsync();
            var inactiveProducts = await GetProductResponses(products);
            return Ok(inactiveProducts);
        }

        private async Task<List<object>> GetProductResponses(IEnumerable<Product> products)
        {
            var productResponses = new List<object>();

            foreach (var p in products)
            {
                var categories = await _categoryService.GetCategoriesByIdsAsync(p.CategoryIds);
                productResponses.Add(new
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
                    Categories = categories,
                    Image = ConvertImageFromBase64(p.ImageBase64)
                });
            }

            return productResponses;
        }

        private string ConvertImageFromBase64(string? base64Image)
        {
            return string.IsNullOrEmpty(base64Image) ? string.Empty : $"data:image/jpeg;base64,{base64Image}";
        }



        [Authorize(Roles = "Vendor")]
        [HttpGet("vendor/{id}")]
        public async Task<IActionResult> GetVendorProductById(string id)
        {
            var vendorId = User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(vendorId))
            {
                return Unauthorized("Vendor ID not found in token.");
            }

            var product = await _productService.GetVendorProductByIdAsync(id, vendorId);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            return Ok(product);
        }

        [Authorize(Roles = "Vendor")]
        [HttpGet("vendor/products")]
        public async Task<IActionResult> GetVendorProducts()
        {
            var vendorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(vendorId))
            {
                return Unauthorized("Vendor ID not found in token.");
            }

            // Fetch only the products that belong to the authenticated vendor
            var products = await _productService.GetVendorProductsAsync(vendorId);

            if (products == null || !products.Any())
            {
                return Ok(new List<object>());
            }

            var productResponses = await GetProductResponses(products);
            return Ok(productResponses);
        }


        // [Authorize(Roles = "Administrator,Customer")]
        // [HttpGet("singleProduct/{id}")]
        // public async Task<IActionResult> GetProductByIdAsync(string id)
        // {
        //     var product = await _productService.GetProductByIdAsync(id);
        //     if (product == null)
        //     {
        //         return NotFound("Product not found.");
        //     }

        //     if (User.IsInRole("Customer") && !product.IsActive)
        //     {
        //         return NotFound("Product is not available.");
        //     }

        //     // Ensure CategoryIds is not null
        //     if (product.CategoryIds == null || !product.CategoryIds.Any())
        //     {
        //         return BadRequest("Product has no categories.");
        //     }

        //     var categories = await _categoryService.GetCategoriesByIdsAsync(product.CategoryIds);
        //     var vendor = await _userService.FindVendorByIdAsync(product.VendorId);
        //     if (vendor == null)
        //     {
        //         return NotFound("Vendor not found.");
        //     }

        //     var ratingsAndComments = await _userService.GetVendorRatingsAndCommentsAsync(product.VendorId);

        //     var productResponse = new
        //     {
        //         product.Id,
        //         product.Name,
        //         product.Description,
        //         product.Price,
        //         product.Quantity,
        //         product.Stock,
        //         product.IsActive,
        //         product.LowStockThreshold,
        //         product.VendorId,
        //         Vendor = new
        //         {
        //             vendor.Id,
        //             vendor.VendorName,
        //             vendor.VendorDescription,
        //             AverageRating = ratingsAndComments.Select(r => new
        //             {
        //                 r.CustomerId,
        //                 r.Rating,
        //                 r.Comment,
        //                 r.CreatedAt
        //             })
        //         },
        //         Categories = categories,
        //         Image = ConvertImageFromBase64(product.ImageBase64)
        //     };

        //     return Ok(productResponse);
        // }
        [Authorize(Roles = "Administrator,Customer,Vendor")]
        [HttpGet("singleProduct/{id}")]
        public async Task<IActionResult> GetProductByIdAsync(string id)
        {
            // Fetch the product
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            // Check if product is active for customers
            if (User.IsInRole("Customer") && !product.IsActive)
            {
                return NotFound("Product is not available.");
            }

            // Validate category IDs
            if (product.CategoryIds == null || !product.CategoryIds.Any())
            {
                return BadRequest("Product has no categories.");
            }

            // Validate vendor ID
            if (string.IsNullOrEmpty(product.VendorId))
            {
                return BadRequest("Product has no vendor associated.");
            }

            // Fetch categories
            var categories = await _categoryService.GetCategoriesByIdsAsync(product.CategoryIds);
            if (categories == null || !categories.Any())
            {
                return NotFound("Categories not found.");
            }

            // Fetch vendor details
            var vendor = await _userService.FindVendorByIdAsync(product.VendorId);
            if (vendor == null)
            {
                return NotFound("Vendor not found.");
            }

            // Fetch vendor ratings and comments
            var ratingsAndComments = await _userService.GetVendorRatingsAndCommentsAsync(product.VendorId);
            if (ratingsAndComments == null)
            {
                ratingsAndComments = new List<VendorRating>(); // Use an empty list if null
            }

            // Prepare the product response with necessary details
            var productResponse = new
            {
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Quantity,
                product.Stock,
                product.IsActive,
                product.LowStockThreshold,
                product.VendorId,

                Vendor = new
                {
                    vendor.Id,
                    vendor.VendorName,
                    vendor.VendorDescription,
                    AverageRating = ratingsAndComments.Any() ? (decimal)ratingsAndComments.Average(r => r.Rating) : 0,

                    // Await each task to get the customer details without the task metadata
                    RatingsAndComments = await Task.WhenAll(ratingsAndComments.Select(async r =>
                    {
                        var customer = await _userService.FindByIdAsync(r.CustomerId);
                        return new
                        {
                            CustomerFirstName = customer?.FirstName,
                            CustomerLastName = customer?.LastName,
                            r.Rating,
                            r.Comment,
                            r.CreatedAt
                        };
                    }))
                },
                Categories = categories,
                Image = ConvertImageFromBase64(product.ImageBase64)
            };

            return Ok(productResponse);

        }


        [Authorize(Roles = "Vendor")]
        [HttpGet("vendor/lowstock")]
        public async Task<IActionResult> GetVendorLowStockProducts()
        {
            var vendorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(vendorId))
            {
                return Unauthorized("Vendor ID not found in token.");
            }

            // Fetch only the low-stock products for the authenticated vendor
            var lowStockProducts = await _productService.GetVendorLowStockProductsAsync(vendorId);

            if (lowStockProducts == null || !lowStockProducts.Any())
            {
                return Ok(new List<object>()); // Return empty list if no low-stock products found
            }

            var productResponses = await GetProductResponses(lowStockProducts);
            return Ok(productResponses);
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("admin/lowstock")]
        public async Task<IActionResult> GetAllLowStockProducts()
        {
            // Fetch all low-stock products
            var lowStockProducts = await _productService.GetAllLowStockProductsAsync();

            if (lowStockProducts == null || !lowStockProducts.Any())
            {
                return Ok(new List<object>()); // Return empty list if no low-stock products found
            }

            var productResponses = await GetProductResponses(lowStockProducts);
            return Ok(productResponses);
        }


    }


    public class ProductResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public int LowStockThreshold { get; set; }
        public string VendorId { get; set; }
        public VendorResponse Vendor { get; set; }
        public List<CategoryResponse> Categories { get; set; } // You might create a CategoryResponse model for this
        public string Image { get; set; }
    }

    public class VendorResponse
    {
        public string Id { get; set; }
        public string VendorName { get; set; }
        public string VendorDescription { get; set; }
        public decimal AverageRating { get; set; }
        public List<RatingResponse> RatingsAndComments { get; set; }
    }

    public class RatingResponse
    {
        public string CustomerId { get; set; }
        public decimal Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CategoryResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        // Add other category fields as needed
    }



}

