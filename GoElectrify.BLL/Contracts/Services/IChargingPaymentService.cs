using GoElectrify.BLL.Dtos.ChargingSession;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IChargingPaymentService
    {
        Task<PaymentReceiptDto> CompletePaymentAsync(
            int userId, int sessionId, CompletePaymentRequest dto, CancellationToken ct);
    }
}
