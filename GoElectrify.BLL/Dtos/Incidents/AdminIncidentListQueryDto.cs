using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Incidents
{
    public class AdminIncidentListQueryDto
    {
        public int? StationId { get; set; }
        public string? Status { get; set; }
        public string? Severity { get; set; }

        public DateTime? FromReportedAt { get; set; }
        public DateTime? ToReportedAt { get; set; }

        /// <summary>
        /// Từ khóa tìm kiếm trên Title/Description.
        /// Ví dụ: "overheat", "charger", "leak"...
        /// </summary>
        public string? Keyword { get; set; }  

        /// <summary>Bỏ qua N dòng đầu (phân trang phía client)</summary>
        public int Skip { get; set; } = 0;

        private int _take = 50;

        /// <summary>
        /// Giới hạn số dòng lấy mỗi lần (<= 200 để bảo vệ DB).
        /// </summary>
        public int Take
        {
            get => _take;
            set => _take = (value <= 0 || value > 200) ? 50 : value;
        }
    }
}
