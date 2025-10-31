using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.WalletSubscription
{
    public class PurchaseSubscriptionRequestDto
    {
        public int SubscriptionId { get; set; }
        public DateTime? StartDate { get; set; }  // null -> DateTime.UtcNow
    }
}
