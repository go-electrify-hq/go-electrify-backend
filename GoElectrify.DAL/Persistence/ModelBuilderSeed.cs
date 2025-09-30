using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.DAL.Persistence
{
    public static class ModelBuilderSeed
    {
        public static void Seed(this ModelBuilder modelBuilder)
        {
            // ===== CONNECTOR TYPES =====
            modelBuilder.Entity<ConnectorType>().HasData(
                new ConnectorType { Id = 1, Name = "CSS1",     Description = "DC fast (Combo 1)",             MaxPowerKw = 200 },
                new ConnectorType { Id = 2, Name = "CSS2",     Description = "DC fast (Combo 2)",             MaxPowerKw = 350 },
                new ConnectorType { Id = 3, Name = "Type1-AC", Description = "SAE J1772 (AC)",                MaxPowerKw = 7   },
                new ConnectorType { Id = 4, Name = "Type2-AC", Description = "IEC 62196-2 Type 2 (Mennekes)", MaxPowerKw = 22  },
                new ConnectorType { Id = 5, Name = "CHAdeMO",  Description = "DC fast (legacy/JDM)",          MaxPowerKw = 62  }
            );

            // ===== VEHICLE MODELS =====
            modelBuilder.Entity<VehicleModel>().HasData(
               new VehicleModel { Id = 200, ModelName = "VinFast VF e34",        MaxPowerKw = 60,  BatteryCapacityKwh = 42.0m },
               new VehicleModel { Id = 201, ModelName = "VinFast VF 3 Eco",      MaxPowerKw = 32,  BatteryCapacityKwh = 19.0m },
               new VehicleModel { Id = 202, ModelName = "VinFast VF 3 Plus",     MaxPowerKw = 37,  BatteryCapacityKwh = 22.0m },
               new VehicleModel { Id = 203, ModelName = "VinFast VF 5 Plus",     MaxPowerKw = 100, BatteryCapacityKwh = 37.0m },
               new VehicleModel { Id = 204, ModelName = "VinFast VF 6 Standard", MaxPowerKw = 150, BatteryCapacityKwh = 59.0m },
               new VehicleModel { Id = 205, ModelName = "VinFast VF 6 Plus",     MaxPowerKw = 160, BatteryCapacityKwh = 59.0m },
               new VehicleModel { Id = 206, ModelName = "VinFast VF 7 Standard", MaxPowerKw = 180, BatteryCapacityKwh = 75.0m },
               new VehicleModel { Id = 207, ModelName = "VinFast VF 7 Plus",     MaxPowerKw = 200, BatteryCapacityKwh = 75.0m },
               new VehicleModel { Id = 208, ModelName = "VinFast VF 8 Eco",      MaxPowerKw = 150, BatteryCapacityKwh = 87.7m },
               new VehicleModel { Id = 209, ModelName = "VinFast VF 8 Plus",     MaxPowerKw = 170, BatteryCapacityKwh = 92.0m },
               new VehicleModel { Id = 210, ModelName = "VinFast VF 9 Eco",      MaxPowerKw = 200, BatteryCapacityKwh = 92.0m },
               new VehicleModel { Id = 211, ModelName = "VinFast VF 9 Plus",     MaxPowerKw = 220, BatteryCapacityKwh = 123.0m }
            );

            // ===== MAPPING =====
            modelBuilder.Entity<VehicleModelConnectorType>().HasData(
                new VehicleModelConnectorType { VehicleModelId = 200, ConnectorTypeId = 3 }, // e34 - AC
                new VehicleModelConnectorType { VehicleModelId = 200, ConnectorTypeId = 1 }, // e34 - CCS
                new VehicleModelConnectorType { VehicleModelId = 201, ConnectorTypeId = 3 }, // VF8 - AC
                new VehicleModelConnectorType { VehicleModelId = 201, ConnectorTypeId = 1 }, // VF8 - CCS
                new VehicleModelConnectorType { VehicleModelId = 202, ConnectorTypeId = 3 }, // VF9 - AC
                new VehicleModelConnectorType { VehicleModelId = 202, ConnectorTypeId = 1 }  // VF9 - CCS
            );
        }
    }
}
 