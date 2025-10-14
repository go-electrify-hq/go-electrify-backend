namespace GoElectrify.BLL.Dto.Subscription
{
    public class SubscriptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public decimal TotalKwh { get; set; }
        public int DurationDays { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
