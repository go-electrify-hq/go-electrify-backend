namespace GoElectrify.BLL.Entities
{
    public class Transaction : BaseEntity
    {
        public int WalletId { get; set; }
        public Wallet Wallet { get; set; } = default!;

        public int? ChargingSessionId { get; set; }       // nếu là thanh toán phiên sạc
        public ChargingSession? ChargingSession { get; set; }

        public decimal Amount { get; set; }               // + nạp, - trừ (decimal(18,2))
        public string Type { get; set; } = default!;      // DEPOSIT | CHARGING | SUBSCRIPTION | REFUND ...
        public string Status { get; set; } = "SUCCEEDED"; // PENDING | SUCCEEDED | FAILED
        public string? Note { get; set; }
    }
}
