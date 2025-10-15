using GoElectrify.BLL.Dtos.WalletTopup;

namespace GoElectrify.BLL.Contracts.Services;

public interface ITopupIntentService
{
    Task<TopupResponseDto> CreateTopupAsync(int walletId, TopupRequestDto dto);
    Task HandleWebhookAsync(PayOSWebhookDto payload);
}
