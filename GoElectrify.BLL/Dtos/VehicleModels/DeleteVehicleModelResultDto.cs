using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.VehicleModels
{
    public class DeleteVehicleModelResultDto
    {
        public int Deleted { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? DeletedIds { get; set; }   // ẩn khi rollback

        public List<int> BlockedIds { get; set; } = new();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<int>? NotFoundIds { get; set; }  // ẩn nếu không có
    }
}
