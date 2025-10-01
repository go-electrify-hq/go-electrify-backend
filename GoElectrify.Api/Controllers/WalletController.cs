using GoElectrify.BLL.Contracts.Services;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class WalletController : ControllerBase
    {
        private readonly ITransactionService _txService;
        public WalletController(ITransactionService txService) => _txService = txService;

        // View Wallet Transactions (Driver)
        [HttpGet("wallet/{walletId}/transactions")]
        public async Task<IActionResult> GetTransactions(
    int walletId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var list = await _txService.GetTransactionsByWalletIdAsync(walletId, from, to);
            return Ok(list);
        }
    }
}
