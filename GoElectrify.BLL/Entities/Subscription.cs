namespace GoElectrify.BLL.Entities
{
    public class Subscription : BaseEntity
    {
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }             // decimal(18,2)
        public decimal TotalKwh { get; set; }          // decimal(12,4)
        public int DurationDays { get; set; }

        public ICollection<WalletSubscription> WalletSubscriptions { get; set; } = new List<WalletSubscription>();
    }
}
