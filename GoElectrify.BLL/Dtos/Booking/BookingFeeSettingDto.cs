namespace GoElectrify.BLL.Dtos.Booking
{
    public sealed class BookingFeeSettingDto
    {
        public string Type { get; set; } = "FLAT"; // "FLAT" | "PERCENT"
        public decimal Value { get; set; } = 0m;   // FLAT: đồng VND; PERCENT: %
    }
}
