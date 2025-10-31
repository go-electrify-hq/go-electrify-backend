using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.Dock
{
    public sealed class StartSessionRequest
    {
        public int SessionId { get; set; }
        public int? TargetSoc { get; set; }
    }
}
