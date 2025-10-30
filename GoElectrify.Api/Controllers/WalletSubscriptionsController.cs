using GoElectrify.Api.Auth;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.WalletSubscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/wallet/{walletId:int}/subscriptions")]
    public class WalletSubscriptionsController : ControllerBase
    {
        private readonly IWalletSubscriptionService _svc;
        public WalletSubscriptionsController(IWalletSubscriptionService svc) => _svc = svc;


        [Authorize] // có thể siết Roles = "User,Driver" nếu muốn
        [HttpGet("wallet/me/subscriptions")]
        public async Task<IActionResult> GetMySubscriptions(CancellationToken ct)
        {
            var userId = User.GetUserId();
            var items = await _svc.GetMineAsync(userId, ct);
            return Ok(items);
        }

        /// Mua gói bằng số dư ví.
        /// Mặc định trả JSON; nếu ?redirect=true -> 303 See Other (Location: "/").
        [HttpPost("purchase")]
        [Authorize]
        public async Task<IActionResult> Purchase(
            int walletId,
            [FromBody] PurchaseSubscriptionRequestDto req,
            [FromQuery] bool redirect = false,
            CancellationToken ct = default)
        {
            try
            {
                var result = await _svc.PurchaseAsync(walletId, req, ct);

                if (redirect)
                {
                    Response.Headers.Location = "/";
                    return StatusCode(StatusCodes.Status303SeeOther);
                }

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });              // "Không tìm thấy ví." | "Không tìm thấy gói đăng ký."
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });            // "Số dư trong ví không đủ để mua gói." | "Dữ liệu gói đăng ký không hợp lệ."
            }
        }
    }
}
