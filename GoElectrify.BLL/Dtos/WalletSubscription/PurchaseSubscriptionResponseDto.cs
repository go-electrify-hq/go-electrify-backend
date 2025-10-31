using GoElectrify.BLL.Dto.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.WalletSubscription
{
    public class PurchaseSubscriptionResponseDto
    {
        public WalletSubscriptionDto WalletSubscription { get; set; } = default!;
        public WalletTransactionDto Transaction { get; set; } = default!;
    }
}
