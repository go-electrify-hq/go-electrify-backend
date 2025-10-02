namespace GoElectrify.BLL.Dto.Notification
{
    public class NotificationDto
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;   // booking, user, charging
        public DateTime CreatedAt { get; set; }
    }
}
