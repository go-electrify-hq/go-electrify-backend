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
        private readonly IConfiguration _config;

        public PayOSWebhookController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook()
        {
            Request.EnableBuffering();

            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            Console.WriteLine("Webhook received: " + rawBody);

            // Signature
            var signature =
                Request.Headers["x-signature"].FirstOrDefault()
                ?? Request.Headers["X-Signature"].FirstOrDefault();

            if (signature == null)
            {
                Console.WriteLine("Missing signature");
                return Unauthorized(new { code = "401", desc = "Missing signature" });
            }

            // Checksum key
            var secretKey = Environment.GetEnvironmentVariable("PayOS__ChecksumKey");
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                Console.WriteLine("Missing PayOS checksum key");
                return StatusCode(500, "Missing ChecksumKey");
            }

            // Verify Signature
            if (!VerifyPayOSSignature(rawBody, signature, secretKey))
            {
                Console.WriteLine($"Invalid signature\nReceived: {signature}\n");
                return Unauthorized(new { code = "401", desc = "Invalid signature" });
            }

            Console.WriteLine("Signature is valid");

            try
            {
                var json = JsonDocument.Parse(rawBody);
                var data = json.RootElement.GetProperty("data");

                long orderCode = data.GetProperty("orderCode").GetInt64();
                decimal amount = data.GetProperty("amount").GetDecimal();
                string code = data.GetProperty("code").GetString() ?? string.Empty;

                if (json.RootElement.TryGetProperty("code", out var rootCodeEl))
                {
                    var rootCode = rootCodeEl.GetString();
                    if (!string.IsNullOrWhiteSpace(rootCode))
                        code = rootCode;
                }

                if (code == "00") // thành công
                {
                    var intent = _db.TopupIntents.FirstOrDefault(t => t.OrderCode == orderCode);
                    if (intent == null)
                        return Ok(new { code = "99", desc = "Intent not found" });

                    intent.Status = "SUCCESS";
                    intent.CompletedAt = DateTime.UtcNow;
                    intent.RawWebhook = rawBody;

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
                        Status = "SUCCEEDED",
                        CreatedAt = DateTime.UtcNow,
                        Note = $"PayOS order {orderCode}"
                    };

                    await _db.Transactions.AddAsync(transaction);
                    await _db.SaveChangesAsync();

                    Console.WriteLine($"Wallet {wallet.Id} +{amount}");
                }
                else
                {
                    Console.WriteLine($"Payment failed (code={code})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling webhook: " + ex.Message);
            }

            return Ok(new { code = "00", desc = "ok" });
        }

        private bool VerifyPayOSSignature(string rawBody, string signature, string secretKey)
        {
            if (signature == null) return false;

            using var hmac = new System.Security.Cryptography.HMACSHA256(
                System.Text.Encoding.UTF8.GetBytes(secretKey));

            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawBody));
            var hashString = Convert.ToHexString(hashBytes).ToLower();

            return hashString == signature.ToLower();
        }
    }
}
