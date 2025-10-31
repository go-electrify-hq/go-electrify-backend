namespace GoElectrify.BLL.Dto.Notification
{
    public class NotificationDto
    {
        public string Id { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Message { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string Severity { get; set; } = "LOW";  // LOW | MEDIUM | HIGH | CRITICAL
        public DateTime CreatedAt { get; set; }

        // trạng thái hiển thị
        public bool IsNew { get; set; } = false;       // để hiển thị badge đỏ
        public bool IsUnread { get; set; } = true;     // để hiển thị chấm xanh
    }
}
