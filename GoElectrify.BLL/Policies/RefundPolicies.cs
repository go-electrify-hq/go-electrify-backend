namespace GoElectrify.BLL.Policies
{
    public static class RefundPolicies
    {
        public static readonly TimeSpan CancelRefundWindow = TimeSpan.FromMinutes(15);

        public static string MakeRefundNote(int bookingId, string sourceTag, string? userReason = null)
        {
            var s = Sanitize(userReason);
            return string.IsNullOrEmpty(s)
                ? $"bookingId={bookingId} source={sourceTag}"
                : $"bookingId={bookingId} source={sourceTag} reason=\"{s}\"";
        }

        private static string Sanitize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var cleaned = new string(text.Trim().Where(c => !char.IsControl(c)).ToArray());
            if (cleaned.Length > 200) cleaned = cleaned[..200];
            return cleaned.Replace("\"", "'");
        }
    }
}
