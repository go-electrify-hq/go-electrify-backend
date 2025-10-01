using System;

namespace GoElectrify.BLL.Dto.Wallet
{
    public class WalletTransactionDto
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public int? ChargingSessionId { get; set; }
        public decimal Amount { get; set; }      
        public string Type { get; set; } = default!;
        public string Status { get; set; } = default!;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
