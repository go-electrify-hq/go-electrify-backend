using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class WalletController : ControllerBase
    {
        private readonly ITransactionService _txService;
        private readonly IWalletRepository _walletRepo;
        public WalletController(
        ITransactionService txService,
        ITopupIntentService topupService,
        IWalletRepository walletRepo)
        {
            _txService = txService;
            _walletRepo = walletRepo;
        }

        [HttpGet("wallet/{walletId}/transactions")]
        public async Task<IActionResult> GetTransactions(int walletId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var list = await _txService.GetTransactionsByWalletIdAsync(walletId, from, to);
            return Ok(list);
        }
        [HttpGet("wallet/{walletId}/balance")]
        public async Task<IActionResult> GetBalance(int walletId)
        {
            var wallet = await _walletRepo.GetByIdAsync(walletId);
            if (wallet == null) return NotFound(new { message = "Wallet not found" });
            return Ok(new { walletId, balance = wallet.Balance });
        }
    }
}
