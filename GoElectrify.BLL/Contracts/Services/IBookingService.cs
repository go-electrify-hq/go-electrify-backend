using GoElectrify.BLL.Dto.Booking;
using GoElectrify.BLL.Dtos.Booking;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IBookingService
    {
        Task<BookingDto> CreateAsync(int userId, CreateBookingDto dto, CancellationToken ct);
        Task<bool> CancelAsync(int userId, int bookingId, string? reason, CancellationToken ct);
        Task<BookingDto?> GetAsync(int userId, int bookingId, CancellationToken ct);
        Task<IReadOnlyList<BookingDto>> GetMyAsync(int userId, MyBookingQueryDto query, CancellationToken ct);
        Task<IReadOnlyList<BookingDto>> GetByStationAsync(int stationId, StationBookingQueryDto q, CancellationToken ct);
    }
}
