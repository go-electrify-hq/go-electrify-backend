using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Wallet;
using GoElectrify.BLL.Dtos.WalletSubscription;

namespace GoElectrify.BLL.Services
{
    public class WalletSubscriptionService : IWalletSubscriptionService
    {
        private readonly IWalletSubscriptionRepository _walletSubRepo;
        private readonly ISubscriptionRepository _subRepo;    // lấy Name/Price/DurationDays
        private readonly IWalletRepository _walletRepo;       // lấy ví theo user
        private readonly INotificationMailService _mail;

        public WalletSubscriptionService(
            IWalletSubscriptionRepository walletSubRepo,
            ISubscriptionRepository subRepo,
            IWalletRepository walletRepo,
            INotificationMailService mail)
        {
            _walletSubRepo = walletSubRepo;
            _subRepo = subRepo;
            _walletRepo = walletRepo;
            _mail = mail;
        }

        /// <summary>
        /// Mua gói bằng ví ảo của user hiện tại (KHÔNG nhận walletId từ client).
        /// </summary>
        public async Task<PurchaseSubscriptionResponseDto> PurchaseAsync(
            int userId, PurchaseSubscriptionRequestDto req, CancellationToken ct)
        {
            // 1) Suy ra ví từ user
            var wallet = await _walletRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Tài khoản chưa có ví ảo.");

            // 2) Chuẩn hoá thời điểm kích hoạt
            var startUtc = req.StartDate?.ToUniversalTime() ?? DateTime.UtcNow;

            // 3) Thực hiện mua gói dưới repo (trừ ví, tạo transaction, tạo wallet-sub)
            var (ws, tx) = await _walletSubRepo.PurchaseSubscriptionAsync(
                wallet.Id, req.SubscriptionId, startUtc, ct);

            // 4) Gửi mail (không làm fail purchase nếu lỗi gửi mail)
            try
            {
                if (tx != null && string.Equals(tx.Status, "SUCCEEDED", StringComparison.OrdinalIgnoreCase))
                {
                    // Snapshot gói
                    var plan = await _subRepo.GetByIdAsync(ws.SubscriptionId, ct);

                    // Lấy email theo wallet qua REPO ĐÚNG CHỮ KÝ mà bạn đã định nghĩa
                    var toEmail = await _walletSubRepo.GetUserEmailByWalletAsync(wallet.Id, ct);

                    if (plan != null && !string.IsNullOrWhiteSpace(toEmail))
                    {
                        var orderCode = $"SUB-{tx.Id:D8}"; // Mã đơn
                        await _mail.SendSubscriptionPurchaseSuccessAsync(
                            toEmail: toEmail,
                            planName: plan.Name,
                            price: plan.Price,
                            provider: "Ví GoElectrify",
                            orderCode: orderCode,
                            durationDays: plan.DurationDays,
                            activatedAtUtc: ws.StartDate,
                            ct: ct
                        );
                    }
                }
            }
            catch
            {
                // TODO: log nếu cần; tuyệt đối không ném lỗi ra ngoài
            }

            // 5) Map DTO trả về
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
                ChargingSessionId = tx.ChargingSessionId, // null với purchase
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

        /// <summary>
        /// Danh sách gói đã mua của user hiện tại (suy ra ví, KHÔNG cần walletId).
        /// </summary>
        public Task<IReadOnlyList<WalletSubscriptionListDto>> GetMineAsync(int userId, CancellationToken ct)
            => _walletSubRepo.GetByUserIdAsync(userId, ct);
    }
}
