using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Dto.Notification;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _db;
        public NotificationRepository(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<NotificationDto>> GetDashboardNotificationsAsync()
        {
            var notifications = new List<NotificationDto>();

            // 1. Đặt chỗ mới
            var booking = await _db.Bookings
                .Include(b => b.Station)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();
            if (booking != null)
            {
                notifications.Add(new NotificationDto
                {
                    Title = "Đặt chỗ mới",
                    Message = $"Có một đặt chỗ mới tại trạm {booking.Station.Name}",
                    Type = "booking",
                    CreatedAt = booking.CreatedAt
                });
            }

            // 2. Người dùng mới
            var newUser = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .FirstOrDefaultAsync();
            if (newUser != null)
            {
                notifications.Add(new NotificationDto
                {
                    Title = "Người dùng mới",
                    Message = "Có người dùng mới đăng ký",
                    Type = "user",
                    CreatedAt = newUser.CreatedAt
                });
            }

            // 3. Hoàn thành sạc
            var session = await _db.ChargingSessions
                .Where(cs => cs.Status == "Completed")
                .OrderByDescending(cs => cs.UpdatedAt)
                .FirstOrDefaultAsync();
            if (session != null)
            {
                notifications.Add(new NotificationDto
                {
                    Title = "Hoàn thành sạc",
                    Message = $"Quá trình sạc tại trạm {session.Charger.StationId} đã hoàn thành",
                    Type = "charging",
                    CreatedAt = session.UpdatedAt
                });
            }

            return notifications.OrderByDescending(n => n.CreatedAt).ToList();
        }
    }
}
