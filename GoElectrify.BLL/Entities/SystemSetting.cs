using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Entities
{
    public class SystemSetting : BaseEntity
    {
        public string Key { get; set; } = default!;
        public string Value { get; set; } = default!;
        public int? UpdatedBy { get; set; }
    }
}
