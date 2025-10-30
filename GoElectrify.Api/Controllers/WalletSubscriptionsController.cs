using GoElectrify.Api.Auth;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.WalletSubscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/wallet/subscriptions")]
    [Authorize]
    public class WalletSubscriptionsController : ControllerBase
    {
        private readonly IWalletSubscriptionService _svc;
        public WalletSubscriptionsController(IWalletSubscriptionService svc) => _svc = svc;

        /// <summary>Danh sách gói đã mua của user hiện tại (không cần walletId).</summary>
        [HttpGet]
        public async Task<IActionResult> GetMine(CancellationToken ct)
        {
            var userId = User.GetUserId();
            var items = await _svc.GetMineAsync(userId, ct);
            return Ok(items);
        }

        /// <summary>Mua gói bằng số dư ví ảo của user hiện tại (không cần walletId).</summary>
        [HttpPost("purchase")]
        public async Task<IActionResult> Purchase(
            [FromBody] PurchaseSubscriptionRequestDto req,
            CancellationToken ct = default)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _svc.PurchaseAsync(userId, req, ct);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
