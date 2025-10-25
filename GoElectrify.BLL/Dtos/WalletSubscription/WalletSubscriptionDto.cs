using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.WalletSubscription
{
    public class WalletSubscriptionDto
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public int SubscriptionId { get; set; }
        public string Status { get; set; } = default!; // ACTIVE | EXPIRED | CANCELED
        public decimal RemainingKwh { get; set; }
        public DateTime StartDate { get; set; }  // UTC
        public DateTime EndDate { get; set; }    // UTC
        public DateTime CreatedAt { get; set; }  // UTC
    }
}
