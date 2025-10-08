using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Booking
{
    public sealed class MyBookingQueryDto
    {
        public string? Status { get; set; } // PENDING|CONFIRMED|CANCELED|EXPIRED|CONSUMED
        public DateTime? From { get; set; } // UTC
        public DateTime? To { get; set; }   // UTC
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
