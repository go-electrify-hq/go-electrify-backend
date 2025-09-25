namespace GoElectrify.BLL.Entities
{
    public class Wallet : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = default!;
        public decimal Balance { get; set; }
    }
}
