using GoElectrify.BLL.Contracts;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class WalletController : ControllerBase
    {
        private readonly ITransactionService _txService;
        private readonly IWalletRepository _walletRepo;
        private readonly IWalletAdminService _walletAdminService;
        public WalletController(
        ITransactionService txService,
        ITopupIntentService topupService,
        IWalletAdminService walletAdminService,
        IWalletRepository walletRepo)
        {
            _txService = txService;
            _walletRepo = walletRepo;
            _walletAdminService = walletAdminService;
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
        [Authorize(Roles = "Admin,Staff")]
        [HttpPost("wallet/{walletId}/deposit/manual")]
        public async Task<IActionResult> ManualDeposit(int walletId, [FromBody] ManualDepositRequestDto dto)
        {
            try
            {
                await _walletAdminService.DepositManualAsync(walletId, dto);
                return Ok(new { message = "Manual deposit succeeded", walletId, amount = dto.Amount });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
