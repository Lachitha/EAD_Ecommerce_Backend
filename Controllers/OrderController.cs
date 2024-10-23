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

            // Assign the user ID to the order
            order.UserId = userId;

            try
            {
                var createdOrder = await _orderService.CreateOrderAsync(order);
                return CreatedAtAction(nameof(GetOrderById), new { orderId = createdOrder.Id }, createdOrder);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Request cancellation by customer (id in body)
        [Authorize(Roles = "Customer")]
        [HttpPost("request-cancel")]
        public async Task<IActionResult> RequestCancelOrder([FromBody] CancelOrderRequest cancelOrderRequest)
        {
            if (string.IsNullOrEmpty(cancelOrderRequest.CancellationNote))
            {
                return BadRequest("Cancellation note is required.");
            }

            var existingOrder = await _orderService.FindOrderByIdAsync(cancelOrderRequest.OrderId);
            if (existingOrder == null)
            {
                return NotFound("Order not found.");
            }

            if (existingOrder.Status != OrderStatus.Processing)
            {
                return BadRequest("Only orders in processing can be requested for cancellation.");
            }

            var updatedOrder = await _orderService.RequestCancelOrderAsync(cancelOrderRequest.OrderId, cancelOrderRequest.CancellationNote);
            return Ok(updatedOrder);
        }

        // Approve cancellation by CSR/Admin (id in body)
        [Authorize(Roles = "CSR,Administrator")]
        [HttpPost("approve-cancel")]
        public async Task<IActionResult> ApproveCancelOrder([FromBody] ApproveCancelRequest approveCancelRequest)
        {
            var canceledOrder = await _orderService.ApproveCancelOrderAsync(approveCancelRequest.OrderId);
            if (canceledOrder == null)
            {
                return NotFound("Order not found or cannot be canceled.");
            }

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
                return NotFound(new { message = "No Orders found." });
            }

            return Ok(orders);
        }

        // Get order by ID (id in URL)
        [Authorize(Roles = "Customer,CSR,Administrator,Vendor")]
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(string orderId)
        {
            var order = await _orderService.FindOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = "Order not found." });
            }

            return Ok(order);
        }


        // Vendor marks product as delivered (orderId and productId in body)
        [Authorize(Roles = "Vendor")]
        [HttpPost("mark-product-delivered")]
        public async Task<IActionResult> MarkProductAsDelivered([FromBody] MarkProductDeliveredRequest request)
        {
            var vendorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(vendorId))
            {
                return Unauthorized("User not authenticated.");
            }

            var updatedOrder = await _orderService.MarkOrderItemAsDeliveredAsync(request.OrderId, vendorId, request.ProductId);
            if (updatedOrder == null)
            {
                return NotFound(new { message = "Order or product not found." });
            }

            return Ok(updatedOrder);
        }

        // CSR/Admin marks the entire order as delivered (orderId in body)
        [Authorize(Roles = "CSR,Administrator")]
        [HttpPost("mark-order-delivered")]
        public async Task<IActionResult> MarkOrderAsDelivered([FromBody] MarkOrderDeliveredRequest request)
        {
            var updatedOrder = await _orderService.MarkOrderAsDeliveredByCSRAsync(request.OrderId);
            if (updatedOrder == null)
            {
                return NotFound(new { message = "Order not found or not eligible for marking as delivered." });
            }

            return Ok(updatedOrder);
        }

        // Get all orders (for Admin)
        [Authorize(Roles = "Administrator")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            if (orders == null || orders.Count == 0)
            {
                return NotFound(new { message = "No orders found." });
            }

            return Ok(orders);
        }

        // Get vendor's orders (vendorId in claims)
        [Authorize(Roles = "Vendor")]
        [HttpGet("vendor-orders")]
        public async Task<IActionResult> GetVendorOrders()
        {
            var vendorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(vendorId))
            {
                return Unauthorized("User not authenticated.");
            }

            var orders = await _orderService.FindOrdersByVendorIdAsync(vendorId);
            if (orders == null || orders.Count == 0)
            {
                return NotFound(new { message = "No orders found for this vendor." });
            }

            return Ok(orders);
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("cancellation-requests")]
        public async Task<IActionResult> GetCancellationRequestedOrders()
        {
            var orders = await _orderService.GetCancellationRequestedOrdersAsync();
            if (orders == null || orders.Count == 0)
            {
                return NotFound(new { message = "No cancellation requested orders found." });
            }

            return Ok(orders);
        }
    }

    // Additional models for request bodies
    public class CancelOrderRequest
    {
        public string OrderId { get; set; }
        public string CancellationNote { get; set; }
    }

    public class ApproveCancelRequest
    {
        public string OrderId { get; set; }
    }

    public class GetOrderRequest
    {
        public string OrderId { get; set; }
    }

    public class MarkProductDeliveredRequest
    {
        public string OrderId { get; set; }
        public string ProductId { get; set; }
    }

    public class MarkOrderDeliveredRequest
    {
        public string OrderId { get; set; }
    }
}
