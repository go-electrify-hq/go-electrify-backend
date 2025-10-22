namespace GoElectrify.BLL.Dto.Notification
{
    public class NotificationDto
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;   // booking, user, charging

        // Mức độ nghiêm trọng: LOW | MEDIUM | HIGH | CRITICAL
        public string Severity { get; set; } = "LOW";
        public DateTime CreatedAt { get; set; }

        // Cho biết thông báo này là “mới/chưa đọc” hay không
        public bool IsNew { get; set; }
    }
}
