using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        // Marker: lưu mốc đã “xem” (badge đỏ)
        public bool IsMarker { get; set; }              // true = dòng marker
        public string? MarkerKind { get; set; }         // "LAST_SEEN"
        public DateTime? MarkerValueUtc { get; set; }   // mốc đã xem

        // Read state theo từng thông báo
        public string? NotifKey { get; set; }      // vd: "booking:12", "tx:99", "assign:5:638..."
        public DateTime? ReadAtUtc { get; set; }        // null = chưa đọc
        public User User { get; set; } = default!;
    }
}
