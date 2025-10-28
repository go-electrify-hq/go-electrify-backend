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
                        // Lấy email user theo WalletId (trực tiếp từ DbContext để khỏi đụng repo/DI)
                        var toEmail = await _db.Wallets
                            .Where(w => w.Id == wallet.Id)
                            .Select(w => w.User.Email)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(HttpContext.RequestAborted);

                        if (!string.IsNullOrWhiteSpace(toEmail))
                        {
                            // Ưu tiên: NotificationMailService (nếu đã AddScoped trong Program.cs)
                            var notif = HttpContext.RequestServices.GetService<INotificationMailService>();
                            if (notif != null)
                            {
                                await notif.SendTopupSuccessAsync(
                                    toEmail: toEmail!,
                                    amount: amount,
                                    provider: "PayOS",
                                    orderCode: orderCode,
                                    completedAtUtc: DateTime.UtcNow,
                                    ct: HttpContext.RequestAborted
                                );
                            }
                            else
                            {
                                // Fallback: gửi trực tiếp bằng IEmailSender (HTML đơn giản)
                                var emailSender = HttpContext.RequestServices.GetService<IEmailSender>();
                                if (emailSender != null)
                                {
                                    var vi = new CultureInfo("vi-VN");
                                    var amountStr = string.Format(vi, "{0:C0}", amount);
                                    var atLocal = DateTime.UtcNow.ToLocalTime().ToString("HH:mm dd/MM/yyyy");

                                    var html = $@"
                                    <!doctype html>
                                    <html>
                                      <body style='font-family:Segoe UI,Arial,sans-serif'>
                                        <h2>🎉 Nạp ví thành công</h2>
                                        <p>Bạn vừa nạp <b>{amountStr}</b> vào ví Go Electrify.</p>
                                        <ul>
                                          <li>Mã giao dịch: <b>{orderCode}</b></li>
                                          <li>Thời gian: <b>{atLocal}</b></li>
                                          <li>Nguồn thanh toán: <b>PayOS</b></li>
                                        </ul>
                                        <p>Nếu không phải bạn thực hiện, vui lòng phản hồi email này để được hỗ trợ.</p>
                                        <hr/>
                                        <small>Go Electrify</small>
                                      </body>
                                    </html>";
                                    await emailSender.SendAsync(
                                        toEmail,
                                        "[Go Electrify] Nạp ví thành công",
                                        html,
                                        HttpContext.RequestAborted
                                    );
                                }
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
    }
}
