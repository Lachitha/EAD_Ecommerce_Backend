using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDbConsoleApp.Models;
using MongoDbConsoleApp.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MongoDbConsoleApp.Controllers
{
    [Authorize(Roles = "Customer,CSR,Administrator,Vendor")]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrderController(OrderService orderService)
        {
            _orderService = orderService;
        }

        // Create a new order (Customer only)
        [Authorize(Roles = "Customer")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            if (order?.Items == null || order.Items.Count == 0)
            {
                return BadRequest("Order must contain at least one item.");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated.");
            }

            // Retrieve VendorIds for each product and assign them to the order items
            foreach (var item in order.Items)
            {
                var product = await _orderService.GetProductByIdAsync(item.ProductId); // Assume GetProductByIdAsync fetches a product by its ID
                if (product == null)
                {
                    return NotFound($"Product not found with ID: {item.ProductId}");
                }

                item.VendorId = product.VendorId; // Assign VendorId from the product
            }

            order.UserId = userId; // Set the user ID
            try
            {
                var createdOrder = await _orderService.CreateOrderAsync(order);
                return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, createdOrder);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message); // Return error if product is not available
            }
        }

        // Request cancellation by customer
        [Authorize(Roles = "Customer")]
        [HttpPost("request-cancel/{id}")]
        public async Task<IActionResult> RequestCancelOrder(string id, [FromBody] string cancellationNote)
        {
            if (string.IsNullOrEmpty(cancellationNote))
            {
                return BadRequest("Cancellation note is required.");
            }

            var existingOrder = await _orderService.FindOrderByIdAsync(id);
            if (existingOrder == null)
            {
                return NotFound("Order not found.");
            }

            if (existingOrder.Status != OrderStatus.Processing)
            {
                return BadRequest("Only orders in processing can be requested for cancellation.");
            }

            var updatedOrder = await _orderService.RequestCancelOrderAsync(id, cancellationNote);
            return Ok(updatedOrder);
        }

        // Approve cancellation by CSR/Admin
        [Authorize(Roles = "CSR,Administrator")]
        [HttpPost("approve-cancel/{id}")]
        public async Task<IActionResult> ApproveCancelOrder(string id)
        {
            var canceledOrder = await _orderService.ApproveCancelOrderAsync(id);
            if (canceledOrder == null)
            {
                return NotFound("Order not found or cannot be canceled.");
            }

            // Notify customer
            await _orderService.NotifyCustomerAsync(canceledOrder.UserId, canceledOrder.CancellationNote);

            return Ok(canceledOrder);
        }

        // Get orders placed by the authenticated customer
        [Authorize(Roles = "Customer")]
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetCustomerOrders()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated.");
            }

            var orders = await _orderService.FindOrdersByUserIdAsync(userId);
            if (orders == null || orders.Count == 0)
            {
                return NotFound("No orders found for this user.");
            }

            return Ok(orders);
        }

        // Get order by ID
        [Authorize(Roles = "Customer,CSR,Administrator,Vendor")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(string id)
        {
            var order = await _orderService.FindOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            return Ok(order);
        }

        // Vendor marks product as delivered
        [Authorize(Roles = "Vendor")]
        [HttpPost("mark-product-delivered/{orderId}")]
        public async Task<IActionResult> MarkProductAsDelivered(string orderId, [FromBody] string productId)
        {
            if (string.IsNullOrEmpty(productId))
            {
                return BadRequest("Product ID must be provided.");
            }

            var vendorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(vendorId))
            {
                return Unauthorized("User not authenticated.");
            }

            var updatedOrder = await _orderService.MarkOrderItemAsDeliveredAsync(orderId, vendorId, productId);
            if (updatedOrder == null)
            {
                return NotFound("Order or product not found.");
            }

            return Ok(updatedOrder);
        }

        // CSR/Admin marks the entire order as delivered
        [Authorize(Roles = "CSR,Administrator")]
        [HttpPost("mark-order-delivered/{orderId}")]
        public async Task<IActionResult> MarkOrderAsDelivered(string orderId)
        {
            var updatedOrder = await _orderService.MarkOrderAsDeliveredByCSRAsync(orderId);
            if (updatedOrder == null)
            {
                return NotFound("Order not found or not eligible for marking as delivered.");
            }

            return Ok(updatedOrder);
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            if (orders == null || orders.Count == 0)
            {
                return NotFound("No orders found.");
            }

            return Ok(orders);
        }


    }
}
