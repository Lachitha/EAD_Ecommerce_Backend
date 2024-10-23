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
    public class NotificationController : ControllerBase
    {
        private readonly NotificationService _notificationService;

        public NotificationController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // Get all notifications for the authenticated user
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated.");
            }

            var notifications = await _notificationService.GetNotificationsByUserIdAsync(userId);
            if (notifications == null || notifications.Count == 0)
            {
                return NotFound(new { message = "No notifications found." });
            }

            return Ok(notifications);
        }

        // Mark a specific notification as read
        [HttpPost("mark-as-read/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(string notificationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User not authenticated.");
            }

            var notification = await _notificationService.FindNotificationByIdAsync(notificationId);
            if (notification == null || notification.UserId != userId)
            {
                return NotFound(new { message = "Notification not found or not authorized to view." });
            }

            await _notificationService.MarkAsReadAsync(notificationId);

            return Ok(new { message = "Notification marked as read." });
        }
    }
}
