using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;

namespace GoElectrify.BLL.Services
{
    public class BookingFeeService : IBookingFeeService
    {
        private readonly ISystemSettingRepository _settings;

        public BookingFeeService(ISystemSettingRepository settings)
        {
            _settings = settings;
        }

        public async Task<(string type, decimal value)> GetAsync(CancellationToken ct)
        {
            var type = (await _settings.GetAsync("BOOKING_FEE_TYPE", ct)) ?? "FLAT";
            type = type.Trim().ToUpperInvariant();
            if (type != "FLAT" && type != "PERCENT") type = "FLAT";

            var valStr = await _settings.GetAsync("BOOKING_FEE_VALUE", ct);
            decimal.TryParse(valStr, out var value);
            if (value < 0) value = 0;

            // VND-only: FLAT => đồng nguyên
            if (type == "FLAT")
                value = Math.Round(value, 0, MidpointRounding.AwayFromZero);

            return (type, value);
        }
    }
}
