using System.Security.Cryptography;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Booking;
using GoElectrify.BLL.Dtos.Booking;
using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Services
{
    public sealed class BookingService : IBookingService
    {
        private readonly IBookingRepository _repo;
        private readonly IStationRepository _stations;
        private readonly IVehicleModelRepository _vehicles;
        private const int SLOT_MINUTES = 30;
        private static readonly string[] AllowedStatuses = ["PENDING", "CONFIRMED", "CANCELED", "EXPIRED", "CONSUMED"];

        public BookingService(IBookingRepository repo, IStationRepository stations, IVehicleModelRepository vehicles)
        {
            _repo = repo;
            _stations = stations;
            _vehicles = vehicles;
        }

        public async Task<BookingDto> CreateAsync(int userId, CreateBookingDto dto, CancellationToken ct)
        {
            // Validate input
            if (!await _repo.StationExistsAsync(dto.StationId, ct)) throw new InvalidOperationException("Station not found.");
            if (!await _repo.VehicleSupportsConnectorAsync(dto.VehicleModelId, dto.ConnectorTypeId, ct))
                throw new InvalidOperationException("Vehicle model does not support the selected connector type.");

            var start = dto.ScheduledStart.ToUniversalTime();
            if (start < DateTime.UtcNow.AddMinutes(5))
                throw new InvalidOperationException("ScheduledStart must be at least +5 minutes from now.");

            // Capacity check within [start, start + SLOT)
            var windowStart = start;
            var windowEnd = start.AddMinutes(SLOT_MINUTES);
            var activeBookings = await _repo.CountActiveBookingsAsync(dto.StationId, dto.ConnectorTypeId, windowStart, windowEnd, ct);
            var capacity = await _repo.CountActiveChargersAsync(dto.StationId, dto.ConnectorTypeId, ct);
            if (activeBookings >= capacity)
                throw new InvalidOperationException("No capacity available for this slot.");

            var e = new Booking
            {
                UserId = userId,
                StationId = dto.StationId,
                ConnectorTypeId = dto.ConnectorTypeId,
                VehicleModelId = dto.VehicleModelId,
                ScheduledStart = start,
                InitialSoc = dto.InitialSoc,
                Status = "CONFIRMED",
                Code = GenerateCode()
            };
            await _repo.AddAsync(e, ct);
            return Map(e);
        }

        public async Task<bool> CancelAsync(int userId, int bookingId, string? reason, CancellationToken ct)
        {
            var e = await _repo.GetByIdAsync(bookingId, ct);
            if (e is null || e.UserId != userId) return false;
            if (e.Status is "CANCELED" or "EXPIRED" or "CONSUMED") return true; // idempotent

            // không cho hủy nếu đã vào phiên sạc (CONSUMED) hoặc quá sát giờ? (tùy BR)
            e.Status = "CANCELED";
            e.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(e, ct);
            return true;
        }

        public async Task<BookingDto?> GetAsync(int userId, int bookingId, CancellationToken ct)
        {
            var e = await _repo.GetByIdAsync(bookingId, ct);
            if (e is null || e.UserId != userId) return null;
            ApplyDerivedExpiry(e);
            return Map(e);
        }

        public async Task<IReadOnlyList<BookingDto>> GetMyAsync(int userId, MyBookingQueryDto q, CancellationToken ct)
        {
            var list = await _repo.GetMyAsync(userId, q.Status, q.From, q.To, q.Page, q.PageSize, ct);
            foreach (var b in list) ApplyDerivedExpiry(b);
            return list.Select(Map).ToList();
        }

        private static void ApplyDerivedExpiry(Booking b)
        {
            if (b.Status is "PENDING" or "CONFIRMED")
            {
                var expireAt = b.ScheduledStart.AddMinutes(SLOT_MINUTES + 10); // grace 10'
                if (DateTime.UtcNow >= expireAt) b.Status = "EXPIRED";
            }
        }

        private static string GenerateCode()
        {
            Span<byte> bytes = stackalloc byte[6];
            RandomNumberGenerator.Fill(bytes);

            var s = Convert.ToBase64String(bytes)
                .Replace('+', 'A')
                .Replace('/', 'B');

            return s.ToUpperInvariant();
        }

        public async Task<IReadOnlyList<BookingDto>> GetByStationAsync(int stationId, StationBookingQueryDto q, CancellationToken ct)
        {
            // Validate status nếu có
            if (!string.IsNullOrWhiteSpace(q.Status) && !AllowedStatuses.Contains(q.Status))
                throw new InvalidOperationException("Invalid status filter.");

            // Chuẩn hoá UTC cho khoảng thời gian
            DateTime? ToUtc(DateTime? d) => d.HasValue ? d.Value.ToUniversalTime() : null;
            var fromUtc = ToUtc(q.From);
            var toUtc = ToUtc(q.To);

            var list = await _repo.GetByStationAsync(
                stationId,
                q.Status,
                fromUtc,
                toUtc,
                q.Page <= 0 ? 1 : q.Page,
                (q.PageSize <= 0 || q.PageSize > 200) ? 20 : q.PageSize,
                ct
            );

            // Gán EXPIRED kiểu “derived” trước khi map (đúng logic GetMyAsync)
            foreach (var b in list) ApplyDerivedExpiry(b);

            return list.Select(Map).ToList();
        }

        private static BookingDto Map(Booking e) => new()
        {
            Id = e.Id,
            Code = e.Code,
            Status = e.Status,
            ScheduledStart = e.ScheduledStart,
            InitialSoc = e.InitialSoc,
            StationId = e.StationId,
            ConnectorTypeId = e.ConnectorTypeId,
            VehicleModelId = e.VehicleModelId,
            EstimatedCost = e.EstimatedCost
        };
    }
}
