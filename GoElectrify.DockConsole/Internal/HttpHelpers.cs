using System.Text.Json;

namespace GoElectrify.DockConsole.Internal
{
    public static class HttpHelpers
    {
        public static string? ExtractBearer(HttpRequest req)
        {
            var auth = req.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return auth.Substring("Bearer ".Length).Trim();

            var token = req.Headers["X-Access-Token"].ToString();
            return string.IsNullOrWhiteSpace(token) ? null : token.Trim();
        }

        public static int? ReadInt(JsonElement root, string name)
        {
            if (root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var i))
                return i;
            return null;
        }

        public static decimal? ReadDecimal(JsonElement root, string name)
        {
            if (root.TryGetProperty(name, out var p))
            {
                if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var v)) return v;
                if (p.ValueKind == JsonValueKind.String && Decimal.TryParse(p.GetString(), out var v2)) return v2;
            }
            return null;
        }

        public static double? ReadDouble(JsonElement root, string name)
        {
            if (root.TryGetProperty(name, out var p))
            {
                if (p.ValueKind == JsonValueKind.Number && p.TryGetDouble(out var v)) return v;
                if (p.ValueKind == JsonValueKind.String && Double.TryParse(p.GetString(), out var v2)) return v2;
            }
            return null;
        }
    }
}
