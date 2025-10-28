using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.WalletTopup;
using GoElectrify.BLL.Entities;
using GoElectrify.BLL.Services.Interfaces;
using GoElectrify.DAL.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GoElectrify.BLL.Services;

public class TopupIntentService : ITopupIntentService
{
    private readonly ITopupIntentRepository _topupRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _txRepo;
    private readonly IPayOSService _payos;
    private readonly INotificationMailService _notifMail;
    private readonly ILogger<TopupIntentService> _logger;

    public TopupIntentService(ITopupIntentRepository topupRepo, IWalletRepository walletRepo, ITransactionRepository txRepo, IPayOSService payos, INotificationMailService notifMail, ILogger<TopupIntentService> logger)
    {
        _topupRepo = topupRepo;
        _walletRepo = walletRepo;
        _txRepo = txRepo;
        _payos = payos;
        _notifMail = notifMail;                            // <-- gán
        _logger = logger;
    }

    public async Task<TopupResponseDto> CreateTopupAsync(int walletId, TopupRequestDto dto)
    {
        var baseUrl = "https://api.go-electrify.com";
        var returnUrl = dto.ReturnUrl ??  baseUrl;
        var cancelUrl = dto.CancelUrl ?? baseUrl;
        //if (string.IsNullOrEmpty(returnUrl) ||
        //   (!returnUrl.StartsWith("https://go-electrify.com") && !returnUrl.StartsWith("http://localhost")))
        //    throw new ArgumentException("Invalid return URL");

        //if (string.IsNullOrEmpty(cancelUrl) ||
        //    (!cancelUrl.StartsWith("https://go-electrify.com") && !cancelUrl.StartsWith("http://localhost")))
        //    throw new ArgumentException("Invalid cancel URL");

        var (checkoutUrl, orderCode) = await _payos.CreatePaymentLinkAsync(
            dto.Amount,
            $"GoElectrify-TopUp-User{walletId}",
            returnUrl,
            cancelUrl
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

        // ======= G?I EMAIL: "N?p ví thành công" (sau khi DB ð? c?p nh?t xong) =======
        try
        {
            var userEmail = await _walletRepo.GetUserEmailByWalletAsync(intent.WalletId);
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                await _notifMail.SendTopupSuccessAsync(
                    toEmail: userEmail,
                    amount: intent.Amount,
                    provider: intent.Provider,          // "PayOS"
                    orderCode: intent.OrderCode,
                    completedAtUtc: intent.CompletedAt ?? DateTime.UtcNow
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Send topup success email failed (orderCode={OrderCode})", intent.OrderCode);
            // Không làm h?ng flow webhook
        }
    }
}
