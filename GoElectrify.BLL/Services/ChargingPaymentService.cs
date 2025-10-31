using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.ChargingSession;
using GoElectrify.BLL.Entities;

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

            var energy = s.EnergyKwh;
            if (energy <= 0)
                throw new InvalidOperationException("EnergyKwh must be > 0 to process payment.");

            var charger = await sessionRepo.GetChargerAsync(s.ChargerId, ct)
                ?? throw new InvalidOperationException("Charger not found.");

            // ✅ ép kiểu non-nullable để tránh CS1503
            decimal unitPrice = charger.PricePerKwh
                ?? throw new InvalidOperationException("Charger price_per_kwh is not configured.");

            var wallet = await walletRepo.GetByUserIdAsync(userId)
                ?? throw new InvalidOperationException("Wallet not found.");

            bool preferWallet = dto.PreferWallet ?? false;

            decimal coveredBySubKwh = 0m;
            decimal billedKwh = 0m;
            decimal billedAmount = 0m;
            var txs = new List<Transaction>();

            if (!preferWallet)
            {
                var activeSubs = await walletSubRepo.GetActiveByWalletIdAsync(wallet.Id, DateTime.UtcNow, ct);
                decimal remain = energy;

                foreach (var ws in activeSubs)
                {
                    if (remain <= 0) break;
                    var use = Math.Min(remain, ws.RemainingKwh);
                    if (use <= 0) continue;

                    ws.RemainingKwh -= use;
                    ws.UpdatedAt = DateTime.UtcNow;
                    coveredBySubKwh += use;
                    remain -= use;
                }

                if (coveredBySubKwh > 0)
                {
                    txs.Add(new Transaction
                    {
                        WalletId = wallet.Id,
                        ChargingSessionId = s.Id,
                        Amount = 0m,
                        Type = "CHARGING_FEE",
                        Status = "SUCCEEDED",
                        Note = $"Used {coveredBySubKwh:F2} kWh from subscription for session #{s.Id}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                if (remain > 0)
                {
                    billedKwh = remain;
                    billedAmount = Round2(billedKwh * unitPrice);

                    if (wallet.Balance < billedAmount)
                        throw new InvalidOperationException("Insufficient wallet balance.");

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
                }
            }
            else
            {
                billedKwh = energy;
                billedAmount = Round2(billedKwh * unitPrice);

                if (wallet.Balance < billedAmount)
                    throw new InvalidOperationException("Insufficient wallet balance.");

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
            }

            s.Status = "COMPLETED";
            s.Cost = billedAmount;
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
                PaymentMethod = preferWallet
                    ? "WALLET"
                    : (coveredBySubKwh > 0 && billedKwh > 0 ? "MIXED" : (coveredBySubKwh > 0 ? "SUBSCRIPTION" : "WALLET")),
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
