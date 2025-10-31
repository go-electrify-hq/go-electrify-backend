using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.Dock
{
    public sealed class CompleteSessionRequest
    {
        public decimal EnergyKwh { get; set; }        // tổng kWh
        public int DurationSeconds { get; set; }      // tổng giây
        public int? EndSoc { get; set; }              // SOC cuối (optional)
        public decimal? PricePerKwhOverride { get; set; } // nếu dock/BE muốn override
    }
}
