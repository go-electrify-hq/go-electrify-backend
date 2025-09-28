using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Entities
{
    public class TopupIntent : BaseEntity
    {
        public int WalletId { get; set; }
        public Wallet Wallet { get; set; } = default!;

        public decimal Amount { get; set; }                // decimal(18,2)
        public string Provider { get; set; } = "SEPAY";    // "SEPAY"
        public string ProviderRef { get; set; } = default!;// mã giao dịch phía provider (duy nhất)

        // CREATED | PENDING | SUCCESS | FAILED | EXPIRED
        public string Status { get; set; } = "CREATED";

        public string? QrContent { get; set; }             // dữ liệu QR trả về (nếu muốn lưu)
        public DateTime? ExpiresAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? RawWebhook { get; set; }            // payload đối soát (optional)
    }
}
