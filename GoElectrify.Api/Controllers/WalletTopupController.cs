using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.WalletTopup;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers;

[ApiController]
[Route("api/v1/wallets/{walletId:int}/topups")]
[Tags("Wallet Top-up")]
public class WalletTopupController : ControllerBase
{
    private readonly ITopupIntentService _service;

    public WalletTopupController(ITopupIntentService service)
    {
        _service = service;
    }

    /// <summary>Create a top-up request via PayOS</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TopupResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(int walletId, [FromBody] TopupRequestDto dto)
        => Ok(await _service.CreateTopupAsync(walletId, dto));
}
