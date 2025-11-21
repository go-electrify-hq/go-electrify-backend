using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            Console.WriteLine("Webhook received: " + rawBody);

            var json = JsonDocument.Parse(rawBody);
            string signature = null;

            if (json.RootElement.TryGetProperty("signature", out var sigEl))
                signature = sigEl.GetString();
            else if (json.RootElement.TryGetProperty("data", out var dataEl) &&
                     dataEl.TryGetProperty("signature", out var sigDataEl))
                signature = sigDataEl.GetString();

            if (string.IsNullOrWhiteSpace(signature))
            {
                Console.WriteLine("❌ Missing signature");
                return Unauthorized(new { code = "401", desc = "Missing signature" });
            }

            var secretKey = Environment.GetEnvironmentVariable("PayOS__ChecksumKey");
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                Console.WriteLine("Missing PayOS__ChecksumKey env");
                return StatusCode(500, "Missing ChecksumKey");
            }

            if (!VerifyPayOSSignature(rawBody, signature, secretKey))
            {
                Console.WriteLine($"Invalid signature");
                Console.WriteLine($"Expected HMAC: {ComputeHmac(rawBody, secretKey)}");
                Console.WriteLine($"Received: {signature}");
                return Unauthorized(new { code = "401", desc = "Invalid signature" });
            }

            Console.WriteLine("Signature is valid");

            try
            {
                var data = json.RootElement.GetProperty("data");

                long orderCode = data.GetProperty("orderCode").GetInt64();
                decimal amount = data.GetProperty("amount").GetDecimal();

                string code = data.GetProperty("code").GetString() ?? "99";
                if (code != "00")
                {
                    Console.WriteLine("Payment failed");
                    return Ok(new { code = "00", desc = "ok" });
                }

                var intent = await _db.TopupIntents.FirstOrDefaultAsync(t => t.OrderCode == orderCode);
                if (intent == null)
                    return Ok(new { code = "99", desc = "Intent not found" });

                var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.Id == intent.WalletId);
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
                    Note = $"PayOS {orderCode}",
                    CreatedAt = DateTime.UtcNow
                };

                await _db.Transactions.AddAsync(tx);
                await _db.SaveChangesAsync();

                Console.WriteLine($"Wallet {wallet.Id} +{amount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return Ok(new { code = "00", desc = "ok" });
        }

        private string ComputeHmac(string rawBody, string secretKey)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(
                System.Text.Encoding.UTF8.GetBytes(secretKey));

            return Convert.ToHexString(hmac.ComputeHash(
                System.Text.Encoding.UTF8.GetBytes(rawBody)
            )).ToLower();
        }

        private bool VerifyPayOSSignature(string rawBody, string signature, string secretKey)
        {
            var expected = ComputeHmac(rawBody, secretKey);
            return expected == signature.ToLower();
        }
    }
}
