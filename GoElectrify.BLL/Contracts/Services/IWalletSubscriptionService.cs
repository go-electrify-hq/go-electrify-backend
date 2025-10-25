using GoElectrify.BLL.Dtos.WalletSubscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IWalletSubscriptionService
    {
        Task<PurchaseSubscriptionResponseDto> PurchaseAsync(
            int walletId, PurchaseSubscriptionRequestDto req, CancellationToken ct);
    }
}
