using GoElectrify.Api.Auth;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.Wallet;
using GoElectrify.BLL.Dtos.WalletTopup;
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
        private readonly ITopupIntentService _topupIntentService;
        public WalletController(
        ITransactionService txService,
        ITopupIntentService topupService,
        IWalletAdminService walletAdminService,
        IWalletRepository walletRepo)
        {
            _txService = txService;
            _walletRepo = walletRepo;
            _walletAdminService = walletAdminService;
            _topupIntentService = topupService;
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

        [Authorize]
        [HttpGet("wallet/me/balance")]
        public async Task<IActionResult> GetMyBalance()
        {
            var userId = User.GetUserId(); // đọc từ JWT (sub/uid)
            var wallet = await _walletRepo.GetByUserIdAsync(userId);
            if (wallet == null)
                return NotFound(new { message = "Wallet not found for current user." });

            return Ok(new
            {
                walletId = wallet.Id,
                balance = wallet.Balance
            });
        }

        [Authorize]
        [HttpGet("wallet/me/transactions")]
        public async Task<IActionResult> GetMyTransactions(
       [FromQuery] DateTime? from = null,
       [FromQuery] DateTime? to = null,
       [FromQuery] int page = 1,
       [FromQuery] int pageSize = 20)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 20;

            var userId = User.GetUserId();
            var wallet = await _walletRepo.GetByUserIdAsync(userId);
            if (wallet == null)
                return NotFound(new { message = "Wallet not found for current user." });

            // service hiện tại trả full list theo from/to => tạm thời paging ở controller
            var all = await _txService.GetTransactionsByWalletIdAsync(wallet.Id, from, to);

            var total = all.Count;
            var items = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                walletId = wallet.Id,
                total,
                page,
                pageSize,
                data = items
            });
        }

        [Authorize(Roles = "Driver,Staff,Admin")]
        [HttpPost("wallet/me/topup")]
        public async Task<IActionResult> TopUpMyWallet([FromBody] TopupRequestDto request)
        {
            if (request.Amount < 10000)
                return BadRequest(new { message = "Minimum top-up amount is 10,000 VND." });

            var userId = User.GetUserId();
            if (userId <= 0)
                return Unauthorized(new { message = "Invalid or missing token." });

            var wallet = await _walletRepo.GetByUserIdAsync(userId);
            if (wallet == null)
                return NotFound(new { message = "Wallet not found for current user." });


            var result = await _topupIntentService.CreateTopupAsync(wallet.Id, new TopupRequestDto
            {
                Amount = request.Amount,
                ReturnUrl = request.ReturnUrl,
                CancelUrl = request.CancelUrl,
            });

            return Ok(new
            {
                message = "Top-up intent created successfully.",
                topupIntentId = result.TopupIntentId,
                orderCode = result.OrderCode,
                checkoutUrl = result.CheckoutUrl
            });
        }
    }
}
