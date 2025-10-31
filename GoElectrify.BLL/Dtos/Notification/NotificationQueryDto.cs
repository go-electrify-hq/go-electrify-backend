using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.Notification
{
    public class NotificationQueryDto
    {
        public DateTime? Since { get; set; } = null;          // Mặc định 7 ngày gần nhất
        public int Limit { get; set; } = 20;                  // 1..100
        public IEnumerable<string>? Types { get; set; }       // Lọc theo type (nếu cần)
        public string? MinSeverity { get; set; }              // "LOW|MEDIUM|HIGH|CRITICAL" (nếu bạn có severity)
    }
}
