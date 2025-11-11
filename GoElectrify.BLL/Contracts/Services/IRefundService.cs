using GoElectrify.BLL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IRefundService
    {
        Task<Transaction?> RefundBookingFeeIfNeededAsync(
            int walletId,
            int bookingId,
            string sourceTag,
            string? userReason,
            CancellationToken ct);
    }
}
