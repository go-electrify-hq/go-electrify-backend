using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GoElectrify.BLL.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GoElectrify.BLL.Services
{
    public class PayOSService : IPayOSService
    {
        private readonly string _clientId;
        private readonly string _apiKey;
        private readonly string _checksumKey;
        private readonly HttpClient _http;

        public PayOSService(IConfiguration config, HttpClient http)
        {
            _clientId = config["PayOS:ClientId"] ?? throw new ArgumentNullException(nameof(_clientId));
            _apiKey = config["PayOS:ApiKey"] ?? throw new ArgumentNullException(nameof(_apiKey));
            _checksumKey = config["PayOS:ChecksumKey"] ?? throw new ArgumentNullException(nameof(_checksumKey));
            _http = http;
        }


        private string CreateSignature(Dictionary<string, string> fields)
        {
            var sorted = new SortedDictionary<string, string>(fields);
            var dataStr = string.Join("&", sorted.Select(kv => $"{kv.Key}={kv.Value}"));
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataStr));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public async Task<(string checkoutUrl, long orderCode)> CreatePaymentLinkAsync(
            decimal amount, string description, string returnUrl, string cancelUrl)
        {
            long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var fields = new Dictionary<string, string>
            {
                { "amount", ((long)amount).ToString() },
                { "cancelUrl", cancelUrl },
                { "description", description },
                { "orderCode", orderCode.ToString() },
                { "returnUrl", returnUrl }
            };

            string signature = CreateSignature(fields);

            var payload = new
            {
                amount = (long)amount,
                cancelUrl,
                description,
                orderCode,
                returnUrl,
                signature
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api-merchant.payos.vn/v2/payment-requests");
            req.Content = JsonContent.Create(payload);
            req.Headers.Add("x-client-id", _clientId);
            req.Headers.Add("x-api-key", _apiKey);
            req.Headers.Add("x-idempotency-key", Guid.NewGuid().ToString());

            using var res = await _http.SendAsync(req);
            var responseBody = await res.Content.ReadAsStringAsync();

            Console.WriteLine("==== PayOS raw response ====");
            Console.WriteLine(responseBody);

            if (!res.IsSuccessStatusCode)
                throw new Exception($"PayOS HTTP error: {(int)res.StatusCode} - {responseBody}");

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null)
            {
                var code = root.TryGetProperty("code", out var c) ? c.GetString() : "??";
                var desc = root.TryGetProperty("desc", out var d) ? d.GetString() : "Không rõ lỗi";
                throw new Exception($"PayOS từ chối yêu cầu: Code={code}, Desc={desc}");
            }

            var checkoutUrl = data.GetProperty("checkoutUrl").GetString() ?? "";
            return (checkoutUrl, orderCode);
        }

        public bool VerifySignature(Dictionary<string, object> data, string signature)
        {
            var sorted = new SortedDictionary<string, object>(data);
            var dataStr = string.Join("&", sorted.Select(kv => $"{kv.Key}={kv.Value}"));
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_checksumKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataStr));
            var calculatedSignature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            return calculatedSignature == signature;
        }
    }
}
