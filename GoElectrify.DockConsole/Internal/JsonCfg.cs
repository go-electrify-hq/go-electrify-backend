using System.Text.Json;

namespace GoElectrify.DockConsole.Internal
{
    public static class JsonCfg
    {
        // Dùng để POST/PASS-THROUGH PascalCase sang API gốc (PropertyNamingPolicy = null)
        public static readonly JsonSerializerOptions Pascal = new() { PropertyNamingPolicy = null };
    }
}
