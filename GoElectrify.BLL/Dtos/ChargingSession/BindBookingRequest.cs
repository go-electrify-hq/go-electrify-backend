using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.ChargingSession
{
    public sealed record BindBookingRequest(
        int? BookingId,
        string? BookingCode,
        int? InitialSoc,
        int? TargetSoc
    );
}
