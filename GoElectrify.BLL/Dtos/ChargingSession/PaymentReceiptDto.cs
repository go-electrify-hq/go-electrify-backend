using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Dtos.ChargingSession
{
    public sealed class PaymentReceiptDto
    {
        public int SessionId { get; set; }
        public string Status { get; set; } = default!;
        public decimal EnergyKwh { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CoveredBySubscriptionKwh { get; set; }
        public decimal BilledKwh { get; set; }
        public decimal BilledAmount { get; set; }
        public string PaymentMethod { get; set; } = default!; // SUBSCRIPTION | WALLET | MIXED
        public List<WalletTransactionDto> Transactions { get; set; } = new();
    }

    public sealed class WalletTransactionDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
