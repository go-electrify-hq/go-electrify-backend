using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Booking;
using GoElectrify.BLL.Dtos.Booking;
using GoElectrify.BLL.Entities;
using GoElectrify.BLL.Entities.Enums;
using GoElectrify.BLL.Exceptions;
using GoElectrify.BLL.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Npgsql;
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
        private readonly IChargerRepository _chargers;

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
            IRefundService refundSvc, IChargerRepository chargers)
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
            _chargers = chargers;
        }

        private static DateTime AlignToSlotStartUtc(DateTime utc, int slotMinutes)
        {
            // đảm bảo là UTC
            if (utc.Kind != DateTimeKind.Utc) utc = utc.ToUniversalTime();

            // Làm tròn "lên" tới mốc slot gần nhất (00/30’)
            var floored = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour,
                                       (utc.Minute / slotMinutes) * slotMinutes, 0, DateTimeKind.Utc);
            if (floored < utc) // nếu đã trễ hơn mốc, đẩy lên mốc kế
                floored = floored.AddMinutes(slotMinutes);
            return floored;
        }
        public async Task<BookingDto> CreateAsync(int userId, CreateBookingDto dto, CancellationToken ct)
        {
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

            var nowUtc = DateTime.UtcNow;
            var startUtc = dto.ScheduledStart.Kind == DateTimeKind.Utc
                ? dto.ScheduledStart
                : DateTime.SpecifyKind(dto.ScheduledStart.ToUniversalTime(), DateTimeKind.Utc);

            var slotStart = AlignToSlotStartUtc(startUtc, SLOT_MINUTES);
            var slotEnd = slotStart.AddMinutes(SLOT_MINUTES);

            if (slotStart < nowUtc.AddMinutes(5))
                throw new InvalidOperationException("ScheduledStart must be at least +5 minutes from now.");

            var active = await _repo.CountActiveBookingsAsync(dto.StationId, dto.ConnectorTypeId, slotStart, slotEnd, ct);
            var cap = await _repo.CountActiveChargersAsync(dto.StationId, dto.ConnectorTypeId, ct);

            _logger.LogInformation("CapacityCheck: station={StationId}, conn={Conn}, slot=[{S:o}..{E:o}), active={Active}, cap={Cap}",
                dto.StationId, dto.ConnectorTypeId, slotStart, slotEnd, active, cap);

            if (cap <= 0 || active >= cap)
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
            var candidates = (await _chargers.GetByStationAsync(dto.StationId, ct))
                .Where(c => c.Status == "ONLINE" && c.ConnectorTypeId == dto.ConnectorTypeId)
                .OrderBy(c => c.Id)
                .ToList();
            if (candidates.Count == 0)
                throw new InvalidOperationException("No online charger available.");

            DbUpdateException? lastConflict = null;
            foreach (var chosen in candidates)
            {
                try
                {
                    var e = new Booking
                    {
                        UserId = userId,
                        StationId = dto.StationId,
                        ConnectorTypeId = dto.ConnectorTypeId,
                        VehicleModelId = dto.VehicleModelId,
                        ScheduledStart = slotStart,
                        InitialSoc = dto.InitialSoc,
                        Status = "CONFIRMED",
                        Code = GenerateCode(),
                        ChargerId = chosen.Id 
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
                catch (DbUpdateException ex) when (IsUniqueSeatViolation(ex))
                {
                    lastConflict = ex;
                    continue;
                }
            }

            // Nếu duyệt hết trụ vẫn conflict/hết chỗ
            throw new InvalidOperationException("No capacity available for this slot.");

            // helper local
            static bool IsUniqueSeatViolation(DbUpdateException ex)
                => ex.InnerException is PostgresException pg && pg.SqlState == "23505";
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
            EstimatedCost = e.EstimatedCost,
            ChargerId = e.ChargerId
        };
    }
}
