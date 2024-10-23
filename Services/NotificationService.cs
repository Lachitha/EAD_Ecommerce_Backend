using MongoDB.Driver;
using MongoDbConsoleApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MongoDbConsoleApp.Services
{
    public class NotificationService
    {
        private readonly IMongoCollection<Notification> _notificationCollection;

        public NotificationService(MongoDbService mongoDbService)
        {
            _notificationCollection = mongoDbService.GetCollection<Notification>("Notifications");
        }

        // Create a new notification
        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification), "Notification object cannot be null.");
            }

            // Insert the notification into the collection
            await _notificationCollection.InsertOneAsync(notification);
            return notification;
        }

        // Get notifications by user ID
        public async Task<List<Notification>> GetNotificationsByUserIdAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null or empty.");
            }

            // Find notifications for the specified user
            return await _notificationCollection.Find(n => n.UserId == userId).ToListAsync();
        }

        // Find a specific notification by ID
        public async Task<Notification?> FindNotificationByIdAsync(string notificationId)
        {
            if (string.IsNullOrEmpty(notificationId))
            {
                throw new ArgumentNullException(nameof(notificationId), "Notification ID cannot be null or empty.");
            }

            // Find the notification with the specified ID
            return await _notificationCollection.Find(n => n.Id == notificationId).FirstOrDefaultAsync();
        }

        // Mark notification as read
        public async Task<bool> MarkAsReadAsync(string notificationId)
        {
            if (string.IsNullOrEmpty(notificationId))
            {
                throw new ArgumentNullException(nameof(notificationId), "Notification ID cannot be null or empty.");
            }

            // Update the IsRead field for the notification
            var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
            var result = await _notificationCollection.UpdateOneAsync(n => n.Id == notificationId, update);

            return result.ModifiedCount > 0; // Return true if the update was successful
        }

        // Delete a notification by ID
        public async Task<bool> DeleteNotificationAsync(string notificationId)
        {
            if (string.IsNullOrEmpty(notificationId))
            {
                throw new ArgumentNullException(nameof(notificationId), "Notification ID cannot be null or empty.");
            }

            // Delete the notification with the specified ID
            var result = await _notificationCollection.DeleteOneAsync(n => n.Id == notificationId);
            return result.DeletedCount > 0; // Return true if the deletion was successful
        }

        // Get all notifications (optional, if you need it for admin purposes)
        public async Task<List<Notification>> GetAllNotificationsAsync()
        {
            return await _notificationCollection.Find(_ => true).ToListAsync();
        }
    }
}
