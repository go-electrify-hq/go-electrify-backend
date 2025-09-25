using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Entities
{
    public class Charger : BaseEntity
    {
        public int StationId { get; set; }
        public Station Station { get; set; } = default!;

        public int ConnectorTypeId { get; set; }
        public ConnectorType ConnectorType { get; set; } = default!;

        public string Code { get; set; } = default!;      // mã trụ/QR
        public int PowerKw { get; set; }                   // công suất danh định
        public string Status { get; set; } = "ONLINE";     // ONLINE | OFFLINE | MAINTENANCE

        // Giá—tuỳ mô hình: có thể để ở Station, hoặc override theo Charger
        public decimal? PricePerKwh { get; set; }         // decimal(18,4)

        public ICollection<ChargingSession> ChargingSessions { get; set; } = new List<ChargingSession>();
        public ICollection<ChargerLog> ChargerLogs { get; set; } = new List<ChargerLog>();
    }
}
