using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.ChargerLogs
{
    public sealed record ChargerLogQueryDto(
        int Page,
        int PageSize,
        DateTime? From,
        DateTime? To,
        string[] States,      // đã chuẩn hoá (UPPER) & tách CSV
        string[] ErrorCodes,  // đã chuẩn hoá (UPPER) & tách CSV
        bool Ascending
    );
}
