using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.Incidents
{
    public class AdminIncidentStatusUpdateDto
    {
        public string Status { get; set; } = string.Empty;

        /// <summary>Ghi chú tuỳ chọn, lưu vào audit/timeline nếu có field.</summary>
        public string? Note { get; set; }
    }
}
