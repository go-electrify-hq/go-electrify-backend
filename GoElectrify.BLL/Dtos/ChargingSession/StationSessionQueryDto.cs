using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.ChargingSession
{

    public sealed class StationSessionQueryDto
    {
        public string? Status { get; set; }      // ví dụ: RUNNING|COMPLETED|CANCELED (tuỳ bạn đang dùng)
        public DateTime? From { get; set; }      // lọc theo StartedAt >= From (UTC hoá trong service)
        public DateTime? To { get; set; }        // lọc theo StartedAt < To (UTC hoá trong service)
        public int Page { get; set; } = 1;       // 1-based
        public int PageSize { get; set; } = 20;  // cap 200
    }

}
