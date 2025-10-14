using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Persistence
{
    public static class ModelBuilderSeed
    {
        public static void Seed(this ModelBuilder modelBuilder)
        {

            // Mốc thời gian tĩnh cho seed
            var now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // ===== CONNECTOR TYPES =====
            modelBuilder.Entity<ConnectorType>().HasData(
                new ConnectorType { Id = 1, Name = "CSS1", Description = "DC fast (Combo 1)", MaxPowerKw = 200, CreatedAt = now, UpdatedAt = now },
                new ConnectorType { Id = 2, Name = "CSS2", Description = "DC fast (Combo 2)", MaxPowerKw = 350, CreatedAt = now, UpdatedAt = now },
                new ConnectorType { Id = 3, Name = "Type1-AC", Description = "SAE J1772 (AC)", MaxPowerKw = 7, CreatedAt = now, UpdatedAt = now },
                new ConnectorType { Id = 4, Name = "Type2-AC", Description = "IEC 62196-2 Type 2 (Mennekes)", MaxPowerKw = 22, CreatedAt = now, UpdatedAt = now },
                new ConnectorType { Id = 5, Name = "CHAdeMO", Description = "DC fast (legacy/JDM)", MaxPowerKw = 62, CreatedAt = now, UpdatedAt = now }
            );

            // ===== VEHICLE MODELS =====
            modelBuilder.Entity<VehicleModel>().HasData(
               new VehicleModel { Id = 200, ModelName = "VinFast VF e34", MaxPowerKw = 60, BatteryCapacityKwh = 42.0m, CreatedAt = now, UpdatedAt = now },
               new VehicleModel { Id = 201, ModelName = "VinFast VF 3 Eco", MaxPowerKw = 32, BatteryCapacityKwh = 19.0m, CreatedAt = now, UpdatedAt = now },
               new VehicleModel { Id = 202, ModelName = "VinFast VF 3 Plus", MaxPowerKw = 37, BatteryCapacityKwh = 22.0m, CreatedAt = now, UpdatedAt = now },
               new VehicleModel { Id = 203, ModelName = "VinFast VF 5 Plus", MaxPowerKw = 100, BatteryCapacityKwh = 37.0m, CreatedAt = now, UpdatedAt = now },
               new VehicleModel { Id = 204, ModelName = "VinFast VF 6 Standard", MaxPowerKw = 150, BatteryCapacityKwh = 59.0m, CreatedAt = now, UpdatedAt = now },
               new VehicleModel { Id = 205, ModelName = "VinFast VF 6 Plus", MaxPowerKw = 160, BatteryCapacityKwh = 59.0m, CreatedAt = now, UpdatedAt = now },
               new VehicleModel { Id = 206, ModelName = "VinFast VF 7 Standard", MaxPowerKw = 180, BatteryCapacityKwh = 75.0m, CreatedAt = now, UpdatedAt = now },
               new VehicleModel { Id = 207, ModelName = "VinFast VF 7 Plus", MaxPowerKw = 200, BatteryCapacityKwh = 75.0m, CreatedAt = now, UpdatedAt = now },
               new VehicleModel { Id = 208, ModelName = "VinFast VF 8 Eco", MaxPowerKw = 150, BatteryCapacityKwh = 87.7m, CreatedAt = now, UpdatedAt = now },
               new VehicleModel { Id = 209, ModelName = "VinFast VF 8 Plus", MaxPowerKw = 170, BatteryCapacityKwh = 92.0m, CreatedAt = now, UpdatedAt = now },
               new VehicleModel { Id = 210, ModelName = "VinFast VF 9 Eco", MaxPowerKw = 200, BatteryCapacityKwh = 92.0m, CreatedAt = now, UpdatedAt = now },
               new VehicleModel { Id = 211, ModelName = "VinFast VF 9 Plus", MaxPowerKw = 220, BatteryCapacityKwh = 123.0m, CreatedAt = now, UpdatedAt = now }
            );

            // ===== MAPPING =====
            modelBuilder.Entity<VehicleModelConnectorType>().HasData(
                new VehicleModelConnectorType { VehicleModelId = 200, ConnectorTypeId = 3 },
                new VehicleModelConnectorType { VehicleModelId = 200, ConnectorTypeId = 1 },
                new VehicleModelConnectorType { VehicleModelId = 201, ConnectorTypeId = 3 },
                new VehicleModelConnectorType { VehicleModelId = 201, ConnectorTypeId = 1 },
                new VehicleModelConnectorType { VehicleModelId = 202, ConnectorTypeId = 3 },
                new VehicleModelConnectorType { VehicleModelId = 202, ConnectorTypeId = 1 }
            );

            // ===== ROLES =====
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Driver", CreatedAt = now, UpdatedAt = now },
                new Role { Id = 2, Name = "Staff", CreatedAt = now, UpdatedAt = now },
                new Role { Id = 3, Name = "Admin", CreatedAt = now, UpdatedAt = now }
            );

            // ===== SUBSCRIPTIONS =====
            modelBuilder.Entity<Subscription>().HasData(
                new Subscription { Id = 1, Name = "Go Spark – Basic", Price = 360000m, TotalKwh = 100m, DurationDays = 30, CreatedAt = now, UpdatedAt = now },
                new Subscription { Id = 2, Name = "Go Pulse - Family", Price = 690000m, TotalKwh = 200m, DurationDays = 30, CreatedAt = now, UpdatedAt = now },
                new Subscription { Id = 3, Name = "Go Drive – Pro", Price = 3990000m, TotalKwh = 1200m, DurationDays = 30, CreatedAt = now, UpdatedAt = now },
                new Subscription { Id = 4, Name = "Go Flow – Flexible", Price = 190000m, TotalKwh = 50m, DurationDays = 30, CreatedAt = now, UpdatedAt = now }
            );

            // ===== STATIONS =====
            modelBuilder.Entity<Station>().HasData(
                new Station
                {
                    Id = 300,
                    Name = "FPT University",
                    Description = "Nạp đầy năng lượng cho xe, sẵn sàng cho việc học! Trạm sạc xe điện hiện đại ngay trong khuôn viên Đại học FPT. Dành cho sinh viên, giảng viên và khách tham quan, giúp bạn sạc pin tiện lợi, an toàn trong giờ học và làm việc. Lựa chọn xanh cho một khuôn viên đại học thông minh.",
                    Address = "7 Đ. D1, Long Thạnh Mỹ, Thủ Đức, Hồ Chí Minh 700000, Việt Nam",
                    ImageUrl = null,
                    Latitude = 10.84167829167107m,
                    Longitude = 106.81083314772492m,
                    Status = "ACTIVE",
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Station
                {
                    Id = 301,
                    Name = "Nhà Văn hóa Sinh viên TP.HCM",
                    Description = "Điểm sạc lý tưởng cho cộng đồng sinh viên năng động! Trạm sạc xe điện được đặt ngay tại Nhà Văn hóa Sinh viên TP.HCM. Bạn có thể an tâm sạc đầy pin trong khi tham gia các hoạt động, học nhóm hay uống cà phê. Nhanh chóng, an toàn và cực kỳ tiện lợi.",
                    Address = "Số 1 Lưu Hữu Phước, Đông Hoà, Dĩ An, Hồ Chí Minh, Việt Nam",
                    ImageUrl = null,
                    Latitude = 10.876244851905408m,
                    Longitude = 106.80600195446553m,
                    Status = "ACTIVE",
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new Station
                {
                    Id = 302,
                    Name = "Vincom Mega Mall Grand Park",
                    Description = "Mua sắm thả ga, không lo hết pin! Trạm sạc xe điện hiện đại nay đã có mặt tại Vincom Mega Mall Grand Park. Hãy sạc đầy pin cho xe trong lúc bạn và gia đình thỏa sức mua sắm, ăn uống và giải trí. Trải nghiệm tiện ích nhân đôi, cho chuyến đi thêm trọn vẹn.",
                    Address = "TTTM Vincom Mega Mall Grand Park, 88 Phước Thiện, Long Bình, Thủ Đức, Hồ Chí Minh, Việt Nam",
                    ImageUrl = null,
                    Latitude = 10.843429972631098m,
                    Longitude = 106.84260840302923m,
                    Status = "ACTIVE",
                    CreatedAt = now,
                    UpdatedAt = now
                }
            );

            // ===== CHARGERS =====
            modelBuilder.Entity<Charger>().HasData(
                // Station 300
                new Charger { Id = 400, StationId = 300, ConnectorTypeId = 2, Code = "FU-DC1", PowerKw = 150, Status = "ONLINE", PricePerKwh = 6500.0000m, CreatedAt = now, UpdatedAt = now },
                new Charger { Id = 401, StationId = 300, ConnectorTypeId = 4, Code = "FU-AC1", PowerKw = 22, Status = "ONLINE", PricePerKwh = 4500.0000m, CreatedAt = now, UpdatedAt = now },
                new Charger { Id = 402, StationId = 300, ConnectorTypeId = 2, Code = "FU-DC2", PowerKw = 150, Status = "ONLINE", PricePerKwh = 6500.0000m, CreatedAt = now, UpdatedAt = now },
                new Charger { Id = 403, StationId = 300, ConnectorTypeId = 2, Code = "FU-DC3", PowerKw = 150, Status = "ONLINE", PricePerKwh = 6500.0000m, CreatedAt = now, UpdatedAt = now },
                new Charger { Id = 404, StationId = 300, ConnectorTypeId = 4, Code = "FU-AC2", PowerKw = 22, Status = "ONLINE", PricePerKwh = 4500.0000m, CreatedAt = now, UpdatedAt = now },

                // Station 301
                new Charger { Id = 410, StationId = 301, ConnectorTypeId = 2, Code = "SC-DC1", PowerKw = 200, Status = "ONLINE", PricePerKwh = 6500.0000m, CreatedAt = now, UpdatedAt = now },
                new Charger { Id = 411, StationId = 301, ConnectorTypeId = 4, Code = "SC-AC1", PowerKw = 22, Status = "ONLINE", PricePerKwh = 4500.0000m, CreatedAt = now, UpdatedAt = now },

                // Station 302
                new Charger { Id = 420, StationId = 302, ConnectorTypeId = 1, Code = "GP-DC1", PowerKw = 120, Status = "ONLINE", PricePerKwh = 6500.0000m, CreatedAt = now, UpdatedAt = now },
                new Charger { Id = 421, StationId = 302, ConnectorTypeId = 5, Code = "GP-CHA1", PowerKw = 50, Status = "ONLINE", PricePerKwh = 6000.0000m, CreatedAt = now, UpdatedAt = now },
                new Charger { Id = 422, StationId = 302, ConnectorTypeId = 4, Code = "GP-AC1", PowerKw = 22, Status = "ONLINE", PricePerKwh = 4500.0000m, CreatedAt = now, UpdatedAt = now }
            );
        }
    }
}
