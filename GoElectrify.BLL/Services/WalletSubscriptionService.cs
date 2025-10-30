using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Wallet;
using GoElectrify.BLL.Dtos.WalletSubscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Services
{
    public class WalletSubscriptionService : IWalletSubscriptionService
    {
        private readonly IWalletSubscriptionRepository _walletSubRepo;
        private readonly ISubscriptionRepository _subRepo;    // lấy Name/Price/DurationDays của gói
        private readonly IWalletRepository _walletRepo;       // lấy email user theo wallet
        private readonly INotificationMailService _mail;
        public WalletSubscriptionService(
            IWalletSubscriptionRepository walletSubRepo, 
            ISubscriptionRepository subRepo,            // ADD
            IWalletRepository walletRepo,               // ADD
            INotificationMailService mail)
        {
            _walletSubRepo = walletSubRepo;
            _subRepo = subRepo;                         // ADD
            _walletRepo = walletRepo;                   // ADD
            _mail = mail;
        }

        public async Task<PurchaseSubscriptionResponseDto> PurchaseAsync(
           int walletId, PurchaseSubscriptionRequestDto req, CancellationToken ct)
        {
            var startUtc = req.StartDate?.ToUniversalTime() ?? DateTime.UtcNow;

            var (ws, tx) = await _walletSubRepo.PurchaseSubscriptionAsync(
                walletId, req.SubscriptionId, startUtc, ct);

            // ADD: Email "MUA GÓI THÀNH CÔNG" (Ví ảo) — chỉ gửi khi giao dịch OK
            try
            {
                if (tx != null && string.Equals(tx.Status, "SUCCEEDED", StringComparison.OrdinalIgnoreCase))
                {
                    // 1) Lấy snapshot gói (Name/Price/DurationDays)
                    var plan = await _subRepo.GetByIdAsync(ws.SubscriptionId, ct); // adjust tên hàm nếu khác
                                                                                   // 2) Lấy email user theo wallet
                    var toEmail = await _walletRepo.GetUserEmailByWalletAsync(walletId);

                    if (plan != null && !string.IsNullOrWhiteSpace(toEmail))
                    {
                        // Mã đơn nội bộ (có thể dùng tx.Id hoặc Note)
                        var orderCode = $"SUB-{tx.Id:D8}";

                        await _mail.SendSubscriptionPurchaseSuccessAsync(
                            toEmail: toEmail,           // theo yêu cầu: chào "quý khách" → chỉ cần email
                            planName: plan.Name,
                            price: plan.Price,        // GIÁ GÓI = "Số tiền"
                            provider: "Ví GoEletrify",
                            orderCode: orderCode,
                            durationDays: plan.DurationDays, // "Thời gian sử dụng"
                            activatedAtUtc: ws.StartDate,
                            ct: ct
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // không làm fail purchase nếu gửi mail lỗi
                // TODO: log ex
            }

            var wsDto = new WalletSubscriptionDto
            {
                Id = ws.Id,
                WalletId = ws.WalletId,
                SubscriptionId = ws.SubscriptionId,
                Status = ws.Status,
                RemainingKwh = ws.RemainingKwh,
                StartDate = ws.StartDate,
                EndDate = ws.EndDate,
                CreatedAt = ws.CreatedAt
            };

            var txDto = new WalletTransactionDto
            {
                Id = tx.Id,
                WalletId = tx.WalletId,
                ChargingSessionId = tx.ChargingSessionId, // null
                Amount = tx.Amount,                        // số âm
                Type = tx.Type,                            // "SUBSCRIPTION"
                Status = tx.Status,                        // "SUCCEEDED"
                Note = tx.Note,                            // "Mua gói: {sub.Name}"
                CreatedAt = tx.CreatedAt
            };

            return new PurchaseSubscriptionResponseDto
            {
                WalletSubscription = wsDto,
                Transaction = txDto
            };
        }

        public Task<IReadOnlyList<WalletSubscriptionListDto>> GetMineAsync(int userId, CancellationToken ct)
            => _walletSubRepo.GetByUserIdAsync(userId, ct);
    }
}
