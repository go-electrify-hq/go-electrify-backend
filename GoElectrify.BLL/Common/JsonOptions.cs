using System.Text.Json;

namespace GoElectrify.BLL.Common
{
    /// <summary>
    /// Provides shared JSON serializer options to avoid duplication across the BLL layer.
    /// </summary>
    public static class SharedJsonOptions
    {
        /// <summary>
        /// JSON serializer options with Web defaults (camelCase property names).
        /// </summary>
        public static readonly JsonSerializerOptions CamelCase = new(JsonSerializerDefaults.Web);
    }
}
