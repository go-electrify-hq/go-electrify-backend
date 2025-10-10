using GoElectrify.BLL.DTOs.WalletTopup;
using System;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Services;

public interface ITopupIntentService
{
    Task<TopupResponseDto> CreateTopupAsync(int walletId, TopupRequestDto dto);
    Task HandleWebhookAsync(PayOSWebhookDto payload);
}
