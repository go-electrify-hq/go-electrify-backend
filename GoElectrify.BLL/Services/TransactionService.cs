using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Wallet;

namespace GoElectrify.BLL.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _repo;
        public TransactionService(ITransactionRepository repo) => _repo = repo;

        public async Task<IReadOnlyList<WalletTransactionDto>> GetTransactionsByWalletIdAsync(
            int walletId, DateTime? from = null, DateTime? to = null)
        {
            var list = await _repo.GetByWalletIdAsync(walletId, from, to);
            return list.Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                WalletId = t.WalletId,
                ChargingSessionId = t.ChargingSessionId,
                Amount = t.Amount,
                Type = t.Type,
                Status = t.Status,
                Note = t.Note,
                CreatedAt = t.CreatedAt
            }).ToList();
        }
    }
}
