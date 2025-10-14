using System.Text.Json;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.WalletTopup;
using GoElectrify.BLL.Entities;
using GoElectrify.BLL.Services.Interfaces;
using GoElectrify.DAL.Repositories;

namespace GoElectrify.BLL.Services;

public class TopupIntentService : ITopupIntentService
{
    private readonly ITopupIntentRepository _topupRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _txRepo;
    private readonly IPayOSService _payos;

    public TopupIntentService(ITopupIntentRepository topupRepo, IWalletRepository walletRepo, ITransactionRepository txRepo, IPayOSService payos)
    {
        _topupRepo = topupRepo;
        _walletRepo = walletRepo;
        _txRepo = txRepo;
        _payos = payos;
    }

    public async Task<TopupResponseDto> CreateTopupAsync(int walletId, TopupRequestDto dto)
    {
        var locale = dto.locale ?? "vi";
        var (checkoutUrl, orderCode) = await _payos.CreatePaymentLinkAsync(
            dto.Amount,
            "nap tien vao vi",
            $"https://api.go-electrify.com/{locale}/payment/success",
            $"https://api.go-electrify.com/{locale}/payment/cancel"
        );

        var intent = new Entities.TopupIntent
        {
            WalletId = walletId,
            Amount = dto.Amount,
            Provider = "PayOS",
            OrderCode = orderCode,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };
        await _topupRepo.AddAsync(intent);

        return new TopupResponseDto
        {
            TopupIntentId = intent.Id,
            CheckoutUrl = checkoutUrl,
            OrderCode = orderCode
        };
    }

    public async Task HandleWebhookAsync(PayOSWebhookDto payload)
    {
        // Basic success check
        if (payload.Code != "00") return;

        // Optional signature verification placeholder (depends on exact PayOS format)
        // var ok = _payos.VerifySignature(new System.Collections.Generic.Dictionary<string, object> {
        //     { "orderCode", payload.Data.OrderCode },
        //     { "amount", payload.Data.Amount },
        //     { "description", payload.Data.Description }
        // }, payload.Signature);
        // if (!ok) return;

        var intent = await _topupRepo.GetByProviderRefAsync(payload.Data.orderCode);
        if (intent == null || intent.Status == "SUCCESS") return;

        intent.Status = "SUCCESS";
        intent.CompletedAt = DateTime.UtcNow;
        intent.RawWebhook = JsonSerializer.Serialize(payload);
        await _topupRepo.UpdateAsync(intent);

        await _walletRepo.UpdateBalanceAsync(intent.WalletId, intent.Amount);
        await _txRepo.AddAsync(new Transaction
        {
            WalletId = intent.WalletId,
            Amount = intent.Amount,
            Type = "DEPOSIT",
            Status = "SUCCESS",
            Note = $"PayOS order {payload.Data.orderCode}"
        });
    }
}
