using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDbConsoleApp.Models;
using MongoDbConsoleApp.Services;
using System.Threading.Tasks;
using MongoDB.Driver;
namespace MongoDbConsoleApp.Controllers
{
    [ApiController]
    [Route("api/categories")] // Changed to "categories" for clarity
    public class CategoryController : ControllerBase
    {
        private readonly CategoryService _categoryService;

        public CategoryController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            if (category == null)
            {
                return BadRequest("Category cannot be null.");
            }

            try
            {
                await _categoryService.CreateCategoryAsync(category);
                return Ok(new { message = "Category created successfully.", categoryId = category.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message); // Return the error message if a duplicate name is detected
            }
        }

        [Authorize(Roles = "Administrator")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(string id, [FromBody] Category updatedCategory)
        {
            if (updatedCategory == null)
            {
                return BadRequest("Category cannot be null.");
            }

            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound("Category not found.");
            }

            updatedCategory.Id = id; // Ensure the category ID matches the one being updated
            await _categoryService.UpdateCategoryAsync(id, updatedCategory);
            return Ok(new { message = "Category updated successfully." });
        }

        [Authorize(Roles = "Administrator")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(string id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound("Category not found.");
            }

            await _categoryService.DeleteCategoryAsync(id);
            return Ok(new { message = "Category deleted successfully." });
        }

        [Authorize(Roles = "Administrator,Customer")]
        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        [Authorize(Roles = "Administrator,Customer")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(string id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound("Category not found.");
            }

            return Ok(category);
        }
    }
}
