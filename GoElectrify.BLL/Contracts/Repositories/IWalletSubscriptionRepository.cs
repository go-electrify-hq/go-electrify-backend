using GoElectrify.BLL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IWalletSubscriptionRepository
    {
        Task<(WalletSubscription WalletSub, Transaction Tx)> PurchaseSubscriptionAsync(
            int walletId, int subscriptionId, DateTime startUtc, CancellationToken ct);
    }
}
