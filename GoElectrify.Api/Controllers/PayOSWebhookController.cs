using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
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
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            Console.WriteLine("=== RAW BODY START ===");
            Console.WriteLine(rawBody);
            Console.WriteLine("=== RAW BODY END ===");


            var json = JsonDocument.Parse(rawBody);

            string signature = null;
            if (json.RootElement.TryGetProperty("signature", out var sigEl))
                signature = sigEl.GetString();

            if (string.IsNullOrWhiteSpace(signature))
            {
                Console.WriteLine("❌ Missing signature");
                return Unauthorized(new { code = "401", desc = "Missing signature" });
            }

            var secretKey = Environment.GetEnvironmentVariable("PayOS__ChecksumKey");
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                Console.WriteLine("Missing PayOS__ChecksumKey ENV");
                return StatusCode(500, "Missing ChecksumKey");
            }
            if (!json.RootElement.TryGetProperty("data", out var dataEl))
            {
                Console.WriteLine("No data field");
                return Ok(new { code = "00", desc = "ok" });
            }

            var dict = new SortedDictionary<string, string>();
            foreach (var p in dataEl.EnumerateObject())
            {
                dict[p.Name] = p.Value.GetString() ?? "";
            }

            var dataStr = string.Join("&", dict.Select(kv => $"{kv.Key}={kv.Value}"));

            Console.WriteLine("Data string used for HMAC:");
            Console.WriteLine(dataStr);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var expected = BitConverter.ToString(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(dataStr))
            ).Replace("-", "").ToLower();

            Console.WriteLine($"Expected: {expected}");
            Console.WriteLine($"Received: {signature}");

            if (expected != signature.ToLower())
            {
                Console.WriteLine("Invalid signature");
                return Unauthorized(new { code = "401", desc = "Invalid signature" });
            }

            Console.WriteLine("Signature verified OK");

            try
            {
                long orderCode = dataEl.GetProperty("orderCode").GetInt64();
                decimal amount = dataEl.GetProperty("amount").GetDecimal();

                var intent = await _db.TopupIntents
                    .FirstOrDefaultAsync(t => t.OrderCode == orderCode);

                if (intent == null)
                    return Ok(new { code = "99", desc = "Intent not found" });

                var wallet = await _db.Wallets
                    .FirstOrDefaultAsync(w => w.Id == intent.WalletId);

                if (wallet == null)
                    return Ok(new { code = "98", desc = "Wallet not found" });

                intent.Status = "SUCCESS";
                intent.CompletedAt = DateTime.UtcNow;
                intent.RawWebhook = rawBody;

                wallet.Balance += amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                var tx = new Transaction
                {
                    WalletId = wallet.Id,
                    Amount = amount,
                    Type = "DEPOSIT",
                    Status = "SUCCEEDED",
                    Note = $"PayOS Banking order {orderCode}",
                    CreatedAt = DateTime.UtcNow
                };

                await _db.Transactions.AddAsync(tx);
                await _db.SaveChangesAsync();

                Console.WriteLine($"💰 Wallet {wallet.Id} +{amount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Webhook error: " + ex.Message);
            }

            return Ok(new { code = "00", desc = "ok" });
        }
    }
}
