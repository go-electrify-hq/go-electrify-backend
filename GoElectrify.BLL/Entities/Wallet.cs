namespace GoElectrify.BLL.Entities
{
    public class Wallet : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = default!;
        public decimal Balance { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<WalletSubscription> WalletSubscriptions { get; set; } = new List<WalletSubscription>();
        public ICollection<TopupIntent> TopupIntents { get; set; } = new List<TopupIntent>();
    }
}
