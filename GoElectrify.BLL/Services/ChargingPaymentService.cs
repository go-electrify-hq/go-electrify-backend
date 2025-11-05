using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.ChargingSession;
using GoElectrify.BLL.Entities;
using GoElectrify.BLL.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Services
{
    public sealed class ChargingPaymentService(
        IChargingSessionRepository sessionRepo,
        IWalletRepository walletRepo,
        ITransactionRepository txRepo,
        IWalletSubscriptionRepository walletSubRepo
    ) : IChargingPaymentService
    {
        public async Task<PaymentReceiptDto> CompletePaymentAsync(
            int userId, int sessionId, CompletePaymentRequest dto, CancellationToken ct)
        {
            var s = await sessionRepo.GetSessionAsync(sessionId, ct)
                ?? throw new InvalidOperationException("Session not found.");
            if (s.EndedAt != null)
                throw new InvalidOperationException("Session already ended.");

            s.EndedAt = DateTime.UtcNow;
            if (dto.FinalSoc.HasValue) s.FinalSoc = dto.FinalSoc.Value;
            s.DurationSeconds = (int)Math.Max(0, (s.EndedAt.Value - s.StartedAt).TotalSeconds);
            var energy = s.EnergyKwh;
            if (energy <= 0)
                throw new InvalidOperationException("EnergyKwh must be > 0 to process payment.");

            var charger = await sessionRepo.GetChargerAsync(s.ChargerId, ct)
                ?? throw new InvalidOperationException("Charger not found.");
            decimal unitPrice = charger.PricePerKwh
                ?? throw new InvalidOperationException("Charger price_per_kwh is not configured.");

            var wallet = await walletRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Wallet not found.");

            var method = dto.Method?.Trim().ToUpperInvariant() ?? "SUBSCRIPTION";

            decimal coveredBySubKwh = 0m;
            decimal billedKwh = 0m;
            decimal billedAmount = 0m;
            var txs = new List<Transaction>();

            if (method == "WALLET")
            {
                // ===== Thanh toán bằng ví =====
                billedKwh = energy;
                billedAmount = Round2(billedKwh * unitPrice);

                if (wallet.Balance < billedAmount)
                    throw BusinessRuleException.WalletInsufficient(billedAmount - wallet.Balance);

                wallet.Balance -= billedAmount;

                txs.Add(new Transaction
                {
                    WalletId = wallet.Id,
                    ChargingSessionId = s.Id,
                    Amount = -billedAmount,
                    Type = "CHARGING",
                    Status = "SUCCEEDED",
                    Note = $"Charged {billedKwh:F2} kWh @ {unitPrice} VND/kWh",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                s.Status = "COMPLETED";
                s.Cost = billedAmount;
            }
            else // SUBSCRIPTION
            {
                // ===== Thanh toán bằng gói =====
                var activeSubs = await walletSubRepo.GetActiveByWalletIdAsync(wallet.Id, DateTime.UtcNow, ct);
                var totalAvail = activeSubs.Sum(ws => ws.RemainingKwh);

                if (totalAvail < energy)
                    throw BusinessRuleException.SubscriptionInsufficient(energy, totalAvail);

                var usages = new List<(int WalletSubscriptionId, decimal UsedKwh)>();
                var remain = energy;

                foreach (var ws in activeSubs
                    .OrderBy(x => x.EndDate)
                    .ThenBy(x => x.CreatedAt))
                {
                    if (remain <= 0) break;
                    var use = Math.Min(remain, ws.RemainingKwh);
                    if (use <= 0) continue;

                    ws.RemainingKwh -= use;
                    ws.UpdatedAt = DateTime.UtcNow;

                    coveredBySubKwh += use;
                    remain -= use;
                    usages.Add((ws.Id, use));
                }

                foreach (var u in usages)
                {
                    txs.Add(new Transaction
                    {
                        WalletId = wallet.Id,
                        ChargingSessionId = s.Id,
                        Amount = 0m,
                        Type = "SUBSCRIPTION_USAGE",
                        Status = "SUCCEEDED",
                        Note = $"Used {u.UsedKwh:F2} kWh from WalletSub#{u.WalletSubscriptionId} for session #{s.Id}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                s.Status = "COMPLETED";
                s.Cost = 0m;
                billedKwh = 0m;
                billedAmount = 0m;
            }

            s.UpdatedAt = DateTime.UtcNow;
            await txRepo.AddRangeAsync(txs);
            await sessionRepo.SaveChangesAsync(ct);

            return new PaymentReceiptDto
            {
                SessionId = s.Id,
                Status = s.Status,
                EnergyKwh = energy,
                UnitPrice = unitPrice,
                CoveredBySubscriptionKwh = coveredBySubKwh,
                BilledKwh = billedKwh,
                BilledAmount = billedAmount,
                PaymentMethod = method,
                Transactions = txs.Select(t => new WalletTransactionDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Type = t.Type,
                    Status = t.Status,
                    Note = t.Note,
                    CreatedAt = t.CreatedAt
                }).ToList()
            };
        }

        private static decimal Round2(decimal v)
            => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    }
}
