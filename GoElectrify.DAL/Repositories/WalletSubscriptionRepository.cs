using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Dtos.WalletSubscription;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.DAL.Repositories
{
    public class WalletSubscriptionRepository : IWalletSubscriptionRepository
    {
        private readonly AppDbContext _db;
        public WalletSubscriptionRepository(AppDbContext db) => _db = db;

        public async Task<(WalletSubscription WalletSub, Transaction Tx)> PurchaseSubscriptionAsync(
            int walletId, int subscriptionId, DateTime startUtc, CancellationToken ct)
        {
            // 1) Lấy ví
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == walletId, ct)
                         ?? throw new KeyNotFoundException("Không tìm thấy ví.");

            // 2) Lấy gói đăng ký
            var sub = await _db.Subscriptions.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == subscriptionId, ct)
                      ?? throw new KeyNotFoundException("Không tìm thấy gói đăng ký.");

            // 3) Kiểm tra dữ liệu hợp lệ
            if (sub.Price <= 0 || sub.TotalKwh <= 0 || sub.DurationDays <= 0)
                throw new InvalidOperationException("Dữ liệu gói đăng ký không hợp lệ.");

            // 4) Kiểm tra số dư ví
            if (wallet.Balance < sub.Price)
                throw new InvalidOperationException("Số dư trong ví không đủ để mua gói.");

            // 5) Trừ tiền ví
            wallet.Balance -= sub.Price;
            var now = DateTime.UtcNow;

            // 6) Tạo giao dịch âm tiền (mua gói) – Note mặc định theo tên gói
            var tx = new Transaction
            {
                WalletId = wallet.Id,
                ChargingSessionId = null,
                Amount = sub.Price,
                Type = "SUBSCRIPTION",
                Status = "SUCCEEDED",
                Note = $"Mua gói: {sub.Name}",
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Transactions.Add(tx);

            // 7) Tạo WalletSubscription (ACTIVE)
            var ws = new WalletSubscription
            {
                WalletId = wallet.Id,
                SubscriptionId = sub.Id,
                Status = "ACTIVE",
                RemainingKwh = sub.TotalKwh,
                StartDate = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(startUtc.AddDays(sub.DurationDays), DateTimeKind.Utc),
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.WalletSubscriptions.Add(ws);

            // 8) Lưu tất cả (atomic)
            await _db.SaveChangesAsync(ct);

            return (ws, tx);
        }
        public async Task<List<WalletSubscription>> GetActiveByWalletIdAsync(
       int walletId, DateTime nowUtc, CancellationToken ct)
        {
            nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);

            return await _db.WalletSubscriptions
                .Where(ws => ws.WalletId == walletId
                          && ws.Status == "ACTIVE"
                          && ws.StartDate <= nowUtc && nowUtc <= ws.EndDate
                          && ws.RemainingKwh > 0)
                .OrderBy(ws => ws.EndDate) // ưu tiên gói sắp hết hạn
                .ToListAsync(ct);
        }

        public async Task<string?> GetUserEmailByWalletAsync(int walletId, CancellationToken ct)
        {
            return await _db.Wallets
                .Where(w => w.Id == walletId)
                .Select(w => w.User.Email)
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);
        }


        public async Task<IReadOnlyList<WalletSubscriptionListDto>> GetByUserIdAsync(int userId, CancellationToken ct)
        {
            // Join WalletSubscription -> Wallet (UserId) -> Subscription (Name, Price, TotalKwh)
            return await _db.WalletSubscriptions
                .AsNoTracking()
                .Where(ws => ws.Wallet.UserId == userId)
                .OrderByDescending(ws => ws.StartDate)
                .Select(ws => new WalletSubscriptionListDto
                {
                    Id = ws.Id,
                    SubscriptionId = ws.SubscriptionId,
                    SubscriptionName = ws.Subscription.Name,
                    Price = ws.Subscription.Price,
                    TotalKwh = ws.Subscription.TotalKwh,
                    RemainingKwh = ws.RemainingKwh,
                    Status = ws.Status,                // đã chuẩn hoá UPPER ở config
                    StartDate = ws.StartDate,
                    EndDate = ws.EndDate
                })
                .ToListAsync(ct);
        }

    }
}
