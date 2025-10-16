using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.Booking
{
    public sealed class StationBookingQueryDto
    {
        public string? Status { get; set; }      // PENDING|CONFIRMED|CANCELED|EXPIRED|CONSUMED
        public DateTime? From { get; set; }      // ISO 8601, sẽ chuẩn hoá UTC trong service
        public DateTime? To { get; set; }        // ISO 8601, sẽ chuẩn hoá UTC trong service
        public int Page { get; set; } = 1;       // 1-based
        public int PageSize { get; set; } = 20;  // cap 200
    }
}
