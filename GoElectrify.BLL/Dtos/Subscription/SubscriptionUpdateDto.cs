namespace GoElectrify.BLL.Dto.Subscription
{
    public sealed class SubscriptionUpdateDto
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public decimal? TotalKwh { get; set; }
        public int? DurationDays { get; set; }
    }
}
