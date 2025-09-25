using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Entities
{
    public class WalletSubscription : BaseEntity
    {
        public int WalletId { get; set; }
        public Wallet Wallet { get; set; } = default!;

        public int SubscriptionId { get; set; }
        public Subscription Subscription { get; set; } = default!;

        // Trạng thái gợi ý: ACTIVE | EXPIRED | CANCELED
        public string Status { get; set; } = "ACTIVE"; // sẽ chuẩn hoá UPPERCASE ở layer dưới

        public decimal RemainingKwh { get; set; }      // decimal(12,4)
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
