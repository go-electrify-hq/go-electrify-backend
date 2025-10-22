using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.Notification
{
    public class NotificationQueryDto
    {
        public DateTime? Since { get; set; }

        // Giới hạn số lượng thông báo trả về (1..100, mặc định 20)
        public int Limit { get; set; } = 20;

        // Lọc theo loại thông báo (nếu null hoặc rỗng → lấy tất cả)
        public string[]? Types { get; set; }

        // Lọc theo mức độ nghiêm trọng tối thiểu
        public string? MinSeverity { get; set; } = "LOW";
    }
}
