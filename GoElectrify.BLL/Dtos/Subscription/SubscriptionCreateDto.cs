namespace GoElectrify.BLL.Dto.Subscription
{
    public sealed class SubscriptionCreateDto
    {
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public decimal TotalKwh { get; set; }
        public int DurationDays { get; set; }
    }
}
