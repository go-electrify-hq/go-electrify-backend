using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Subscription
{
    public sealed class SubscriptionUpdateDto
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public decimal? TotalKwh { get; set; }
        public int? DurationDays { get; set; }
    }
}
