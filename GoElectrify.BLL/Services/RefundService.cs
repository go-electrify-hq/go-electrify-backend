using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Entities;
using GoElectrify.BLL.Policies;
using System.Globalization;

namespace GoElectrify.BLL.Services
{
    public sealed class RefundService : IRefundService
    {
        private readonly IWalletRepository _walletRepo;
        private readonly ITransactionRepository _txRepo;
        private readonly ISystemSettingRepository _settingsRepo;

        public RefundService(
            IWalletRepository walletRepo,
            ITransactionRepository txRepo,
            ISystemSettingRepository settingsRepo)
        {
            _walletRepo = walletRepo;
            _txRepo = txRepo;
            _settingsRepo = settingsRepo;
        }

        public async Task<Transaction?> RefundBookingFeeIfNeededAsync(
            int walletId,
            int bookingId,
            string sourceTag,
            string? userReason,
            CancellationToken ct)
        {
            // 1) Lấy BookingFee từ SystemSetting (string -> decimal)
            var raw = await _settingsRepo.GetAsync("BOOKING_FEE_VALUE", ct);
            if (string.IsNullOrWhiteSpace(raw)) return null;

            if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var bookingFee))
            {
                // fallback: thử theo văn hoá máy nếu FE/BE đang lưu kiểu "10000,00"
                if (!decimal.TryParse(raw, out bookingFee)) return null;
            }
            if (bookingFee <= 0) return null;

            // 2) Idempotent theo WalletId + bookingId (tìm trong Note)
            if (await _txRepo.ExistsRefundByBookingIdAsync(walletId, bookingId, ct))
                return await _txRepo.GetRefundByBookingIdAsync(walletId, bookingId, ct);

            // 3) + tiền vào ví (delta dương)
            await _walletRepo.UpdateBalanceAsync(walletId, bookingFee);

            // 4) Ghi transaction
            var now = DateTime.UtcNow;
            var tx = new Transaction
            {
                WalletId = walletId,
                ChargingSessionId = null,         // refund booking: không gắn session
                Amount = bookingFee,              // refund là cộng vào ví
                Type = "REFUND",
                Status = "SUCCEEDED",
                Note = sourceTag switch
                {
                    "CANCEL_BEFORE_WINDOW" => "Hoàn tiền đặt chỗ (Hủy sớm)",
                    "SESSION_PAID" => "Hoàn tiền đặt chỗ (Sau thanh toán)",
                    _ => "Hoàn tiền đặt chỗ"
                },
                CreatedAt = now,
                UpdatedAt = now
            };

            return await _txRepo.CreateAsync(tx, ct);
        }
    }
}
