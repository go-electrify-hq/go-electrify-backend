using System.Text.Json;

namespace GoElectrify.Api.Common
{
    /// <summary>
    /// Provides shared JSON serializer options to avoid duplication across the application.
    /// </summary>
    public static class SharedJsonOptions
    {
        /// <summary>
        /// JSON serializer options with Web defaults (camelCase property names).
        /// </summary>
        public static readonly JsonSerializerOptions CamelCase = new(JsonSerializerDefaults.Web);
    }
}
