using MongoDbConsoleApp.Models;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDbConsoleApp.Services
{
    public class CartService
    {
        private readonly IMongoCollection<Cart> _cartCollection;
        private readonly ProductService _productService; // To interact with products

        public CartService(MongoDbService database, ProductService productService)
        {
            _cartCollection = database.GetCollection<Cart>("Carts");
            _productService = productService;
        }

        // Get the cart by userId
        public async Task<Cart?> GetCartByUserId(string userId)
        {
            return await _cartCollection.Find(cart => cart.UserId == userId).FirstOrDefaultAsync();
        }

        // Add product to cart
        public async Task AddToCart(string userId, string productId, int quantity)
        {
            var cart = await GetCartByUserId(userId) ?? new Cart { UserId = userId };
            var product = await _productService.GetProductByIdAsync(productId); // Use existing method to fetch product

            if (product == null) throw new Exception("Product not found");
            if (quantity <= 0) throw new Exception("Quantity must be greater than zero.");
            if (product.Quantity < quantity) throw new Exception("Insufficient stock available.");

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity; // Increase existing quantity
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    Price = product.Price // Use the product price from ProductService
                });
            }

            await UpdateProductStock(productId, -quantity); // Update stock in Product
            await _cartCollection.ReplaceOneAsync(c => c.UserId == userId, cart, new ReplaceOptions { IsUpsert = true });
        }

        // Remove product from cart
        public async Task RemoveFromCart(string userId, string productId)
        {
            var cart = await GetCartByUserId(userId);
            if (cart == null) throw new Exception("Cart not found");

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem != null)
            {
                await UpdateProductStock(productId, cartItem.Quantity); // Restore stock in Product
                cart.Items.Remove(cartItem);
            }

            await _cartCollection.ReplaceOneAsync(c => c.UserId == userId, cart, new ReplaceOptions { IsUpsert = true });
        }

        // Update product quantity in cart
        public async Task UpdateQuantity(string userId, string productId, int newQuantity)
        {
            var cart = await GetCartByUserId(userId);
            if (cart == null) throw new Exception("Cart not found");

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem == null) throw new Exception("Product not in cart");

            // Update stock based on the new quantity
            if (newQuantity < cartItem.Quantity)
            {
                await UpdateProductStock(productId, cartItem.Quantity - newQuantity); // Add stock if reducing quantity
            }
            else if (newQuantity > cartItem.Quantity)
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null) throw new Exception("Product not found");
                if (product.Quantity < (newQuantity - cartItem.Quantity)) throw new Exception("Insufficient stock available.");
                await UpdateProductStock(productId, newQuantity - cartItem.Quantity); // Deduct stock if increasing quantity
            }

            cartItem.Quantity = newQuantity;

            await _cartCollection.ReplaceOneAsync(c => c.UserId == userId, cart, new ReplaceOptions { IsUpsert = true });
        }

        private async Task UpdateProductStock(string productId, int quantityChange)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null) throw new Exception("Product not found.");

            product.Quantity += quantityChange; // Update product stock
            await _productService.UpdateProductAsync(productId, product); // Persist changes in the product
        }
    }
}
