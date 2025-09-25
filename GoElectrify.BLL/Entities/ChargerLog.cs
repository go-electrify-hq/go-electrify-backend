using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Entities
{
    public class ChargerLog : BaseEntity
    {
        public int ChargerId { get; set; }
        public Charger Charger { get; set; } = default!;

        public DateTime SampleAt { get; set; }            // thời điểm đọc log (UTC)

        // Thông số vận hành (tối giản, có thể mở rộng tuỳ hardware)
        public decimal? Voltage { get; set; }             // V
        public decimal? Current { get; set; }             // A
        public decimal? PowerKw { get; set; }             // kW
        public decimal? SessionEnergyKwh { get; set; }    // kWh đã nạp trong phiên
        public int? SocPercent { get; set; }              // %

        // Trạng thái/ lỗi
        public string? State { get; set; }                // IDLE | CHARGING | FAULT ...
        public string? ErrorCode { get; set; }
    }
}
