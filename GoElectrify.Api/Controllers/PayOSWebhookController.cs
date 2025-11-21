using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/payoswebhook")]
    public class PayOSWebhookController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PayOSWebhookController(AppDbContext db)
        {
            _db = db;
        }
        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook()
        {
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            Console.WriteLine("📩 Webhook received: " + body);

            if (!Request.Headers.TryGetValue("X-Signature", out var signatureHeader))
                return Unauthorized(new { code = "401", desc = "Missing signature" });
            var signature = signatureHeader.ToString();

            var secretKey = Environment.GetEnvironmentVariable("ChecksumKey");
            if (string.IsNullOrWhiteSpace(secretKey))
                return StatusCode(500, "Missing ChecksumKey");

            if (!VerifyPayOSSignature(body, signature, secretKey))
                return Unauthorized(new { code = "401", desc = "Invalid signature" });
            try
            {
                var json = JsonDocument.Parse(body);
                var data = json.RootElement.GetProperty("data");

                long orderCode = data.GetProperty("orderCode").GetInt64();
                decimal amount = data.GetProperty("amount").GetDecimal();
                string code = data.GetProperty("code").GetString() ?? string.Empty;

                if (json.RootElement.TryGetProperty("code", out var rootCodeEl))
                {
                    var rootCode = rootCodeEl.GetString();
                    if (!string.IsNullOrWhiteSpace(rootCode))
                        code = rootCode; // ghi đè code đã lấy từ data
                }

                if (code == "00")
                {
                    var intent = _db.TopupIntents.FirstOrDefault(t => t.OrderCode == orderCode);
                    if (intent == null)
                        return Ok(new { code = "99", desc = "Intent not found" });

                    intent.Status = "SUCCESS";
                    intent.CompletedAt = DateTime.UtcNow;
                    intent.RawWebhook = body;

                    var wallet = _db.Wallets.FirstOrDefault(w => w.Id == intent.WalletId);
                    if (wallet == null)
                        return Ok(new { code = "98", desc = "Wallet not found" });
                    wallet.Balance += amount;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    var transaction = new Transaction
                    {
                        WalletId = wallet.Id,
                        Amount = amount,
                        Type = "DEPOSIT",
                        CreatedAt = DateTime.UtcNow,
                        Note = $"PayOS order {orderCode}"
                    };
                    await _db.Transactions.AddAsync(transaction);

                    await _db.SaveChangesAsync();
                    Console.WriteLine($"Wallet {wallet.Id} +{amount}");

                    // ADD — GỬI EMAIL XÁC NHẬN NẠP VÍ
                    try
                    {
                        // Lấy Email + FullName để format "Họ Tên <email>"
                        var userInfo = await _db.Wallets
                            .Where(w => w.Id == wallet.Id)
                            .Select(w => new { w.User.Email, w.User.FullName })
                            .AsNoTracking()
                            .FirstOrDefaultAsync(HttpContext.RequestAborted);

                        if (!string.IsNullOrWhiteSpace(userInfo?.Email))
                        {
                            var toDisplay = string.IsNullOrWhiteSpace(userInfo.FullName)
                                ? userInfo.Email
                                : $"{userInfo.FullName} <{userInfo.Email}>";

                            var notif = HttpContext.RequestServices.GetService<INotificationMailService>();
                            if (notif != null)
                            {
                                await notif.SendTopupSuccessAsync(
                                    toEmail: toDisplay,
                                    amount: amount,
                                    provider: "PayOS",
                                    orderCode: orderCode,
                                    completedAtUtc: intent.CompletedAt ?? DateTime.UtcNow,
                                    ct: HttpContext.RequestAborted
                                );
                            }
                            else
                            {
                                Console.WriteLine("⚠️ INotificationMailService not registered; skip sending email.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Wallet {wallet.Id} không tìm thấy email user.");
                        }
                    }
                    catch (Exception mailEx)
                    {
                        Console.WriteLine("⚠️ Send email failed: " + mailEx.Message);
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ Payment failed (code={code})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error handling webhook: " + ex.Message);
            }

            return Ok(new { code = "00", desc = "ok" });
        }

        private bool VerifyPayOSSignature(string rawBody, string signature, string secretKey)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(
                System.Text.Encoding.UTF8.GetBytes(secretKey));

            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawBody));
            var hashString = Convert.ToHexString(hashBytes).ToLower();

            return hashString == signature.ToLower();
        }
    }
}
