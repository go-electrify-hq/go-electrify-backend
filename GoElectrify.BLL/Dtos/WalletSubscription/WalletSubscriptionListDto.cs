using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.WalletSubscription
{
    public class WalletSubscriptionListDto
    {
        public int Id { get; set; }
        public int SubscriptionId { get; set; }
        public string SubscriptionName { get; set; } = default!;
        public decimal Price { get; set; }
        public decimal TotalKwh { get; set; }
        public decimal RemainingKwh { get; set; }
        public string Status { get; set; } = default!;        // ACTIVE | EXPIRED | CANCELED
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
