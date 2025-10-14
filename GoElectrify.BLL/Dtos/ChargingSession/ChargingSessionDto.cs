namespace GoElectrify.BLL.Dto.ChargingSession
{
    public sealed class ChargingSessionDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = default!;
        public DateTime StartedAt { get; set; }
        public int ChargerId { get; set; }
        public int StationId { get; set; }          // từ Charger
        public int ConnectorTypeId { get; set; }    // từ Charger
        public int? BookingId { get; set; }
        public int InitialSoc { get; set; }

        /// <summary>Dung lượng pin (kWh) của model xe. Lấy từ VehicleModel.BatteryCapacityKwh.</summary>
        public decimal VehicleBatteryCapacityKwh { get; set; } // từ VehicleModel.BatteryCapacityKwh
        public int VehicleMaxPowerKw { get; set; }  // từ VehicleModel.MaxPowerKw
        public decimal ChargerPowerKw { get; set; }  // từ Charger.PowerKw
        public int ConnectorMaxPowerKw { get; set; }  // từ ConnectorType.MaxPowerKw
        public int? TargetSoc { get; set; }
    }
}
