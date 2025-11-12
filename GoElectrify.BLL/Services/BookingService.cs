using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Booking;
using GoElectrify.BLL.Dtos.Booking;
using GoElectrify.BLL.Entities;
using GoElectrify.BLL.Entities.Enums;
using GoElectrify.BLL.Exceptions;
using GoElectrify.BLL.Policies;
using Microsoft.Extensions.Logging;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace GoElectrify.BLL.Services
{
    public sealed class BookingService : IBookingService
    {
        private readonly IBookingRepository _repo;
        private readonly IStationRepository _stations;
        private readonly IVehicleModelRepository _vehicles;
        private readonly IWalletRepository _wallets;
        private readonly ITransactionRepository _tx;
        private readonly IBookingFeeService _fee;
        private readonly IRefundService _refundSvc;

        // [MAIL]
        private readonly INotificationMailService _notifMail;
        private readonly ILogger<BookingService> _logger;

        private const int SLOT_MINUTES = 30;
        private static readonly string[] AllowedStatuses = ["PENDING", "CONFIRMED", "CANCELED", "EXPIRED", "CONSUMED"];

        public BookingService(IBookingRepository repo, IStationRepository stations, IVehicleModelRepository vehicles, IWalletRepository wallets,
            ITransactionRepository tx,
            IBookingFeeService fee,
            INotificationMailService notifMail,
            ILogger<BookingService> logger,
            IRefundService refundSvc)
        {
            _repo = repo;
            _stations = stations;
            _vehicles = vehicles;
            _wallets = wallets;
            _tx = tx;
            _fee = fee;
            _notifMail = notifMail;
            _logger = logger;
            _refundSvc = refundSvc;
        }

        public async Task<BookingDto> CreateAsync(int userId, CreateBookingDto dto, CancellationToken ct)
        {
            // Validate input
            //if (!await _repo.StationExistsAsync(dto.StationId, ct)) throw new InvalidOperationException("Station not found.");
            //if (!await _repo.VehicleSupportsConnectorAsync(dto.VehicleModelId, dto.ConnectorTypeId, ct))
            //    throw new InvalidOperationException("Vehicle model does not support the selected connector type.");

            var station = await _stations.GetByIdAsync(dto.StationId)
             ?? throw new InvalidOperationException("Station not found.");
            if (station.Status != StationStatus.ACTIVE)
                throw new InvalidOperationException("Station is not available at the moment.");

            if (dto.VehicleModelId.HasValue)
            {
                var ok = await _repo.VehicleSupportsConnectorAsync(dto.VehicleModelId.Value, dto.ConnectorTypeId, ct);
                if (!ok)
                    throw new InvalidOperationException("Vehicle model does not support the selected connector type.");
            }
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

            var (feeType, feeVal) = await _fee.GetAsync(ct);
            decimal estimatedCost = 0m;

            decimal fee;
            if (feeType == "PERCENT")
            {
                var calc = estimatedCost * feeVal / 100m;
                fee = Math.Ceiling(calc);
            }
            else
            {
                fee = Math.Round(feeVal, 0, MidpointRounding.AwayFromZero);
            }
            if (fee < 0) fee = 0;

            if (fee > 0)
            {
                var wallet = await _wallets.GetByUserIdAsync(userId)
                             ?? throw new InvalidOperationException("Wallet not found.");

                if (wallet.Balance < fee)
                    throw new InsufficientFundsException(fee, wallet.Balance);

                // Khuyến nghị: bọc các bước dưới trong transaction DbContext
                wallet.Balance -= fee;
                await _wallets.UpdateAsync(wallet.Id, wallet.Balance);

                await _tx.AddAsync(new Transaction
                {
                    WalletId = wallet.Id,
                    Amount = fee,
                    Type = "BOOKING_FEE",
                    Status = "SUCCEEDED",
                    Note = $"Booking fee"
                });
            }
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

            // ================== [MAIL] gửi email "Đặt chỗ thành công" ==================
            // === EMAIL: Đặt chỗ thành công ===
            try
            {
                var userEmail = await _repo.GetUserEmailAsync(userId, ct);
                if (!string.IsNullOrWhiteSpace(userEmail))
                {
                    // Lấy tên trạm thật; nếu null → fallback
                    var stationName = await _stations.GetNameByIdAsync(e.StationId, ct)
                                     ?? $"Trạm #{e.StationId}";

                    // Booking của bạn hiện chưa có ChargerId → để null
                    string? chargerName = null;

                    await _notifMail.SendBookingSuccessAsync(
                        toEmail: userEmail,
                        bookingCode: e.Code,
                        stationName: stationName,
                        chargerName: chargerName,
                        startTimeUtc: e.ScheduledStart,
                        endTimeUtc: null,
                        ct: ct
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Send booking success email failed (bookingCode={Code})", e.Code);
            }

            // ================== [/MAIL] ==================
            return Map(e);
        }

        public async Task<bool> CancelAsync(int userId, int bookingId, string? reason, CancellationToken ct)
        {
            var booking = await _repo.GetByIdAsync(bookingId, ct);
            if (booking is null) return false;
            if (booking.UserId != userId) return false;

            // đánh dấu hủy
            booking.Status = "CANCELED"; // hoặc BookingStatus.CANCELED nếu bạn dùng constant
            await _repo.UpdateAsync(booking, ct);

            // Hoàn nếu hủy trước 15'
            var refundable = DateTime.UtcNow <= booking.CreatedAt.AddMinutes(15);

            if (refundable)
            {
                var wallet = await _wallets.GetByUserIdAsync(userId);
                if (wallet != null)
                {
                    // reason: lấy từ FE (body.Reason) – truyền nguyên sang RefundService
                    try
                    {
                        await _refundSvc.RefundBookingFeeIfNeededAsync(
                            walletId: wallet.Id,
                            bookingId: booking.Id,
                            sourceTag: "CANCEL_BEFORE_WINDOW",
                            userReason: reason,
                            ct: ct);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Cancel] Refund exception: {ex.Message}");
                        throw;
                    }
                }
            }

                
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
