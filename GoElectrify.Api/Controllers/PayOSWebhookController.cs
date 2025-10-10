using GoElectrify.DAL.Persistence;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using GoElectrify.BLL.Entities;
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
                string code = data.GetProperty("code").GetString();

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
