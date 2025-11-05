using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.ChargingSession
{
    public sealed record SessionLogWindow(DateTime FromUtc, DateTime ToUtc);

    public sealed record StartSessionResult(
        int Id, string? Status, DateTime StartedAt,
        int? TargetSoc, int SocStart, int? BookingId, int ChargerId
    );

    public sealed record BindBookingResult(
        int SessionId, int? BookingId, int? VehicleModelId,
        int SocStart, int? TargetSoc
    );

    public sealed record ChargingSessionLightDto(
        int Id, string? Status, DateTime StartedAt, DateTime? EndedAt,
        int? TargetSoc, int SocStart, int? FinalSoc,
        decimal EnergyKwh, decimal? Cost,
        int? BookingId, int ChargerId, string? AblyChannel
    );

    public sealed record ChargingSessionHistoryItemDto(
        int Id, string? Status, DateTime StartedAt, DateTime? EndedAt,
        int DurationSeconds, int? TargetSoc, int SocStart, int? FinalSoc,
        decimal EnergyKwh, decimal? Cost,
        int? BookingId, int ChargerId, string? AblyChannel
    );

    public sealed record HistoryQueryDto(
        int Page, int PageSize, DateTime? From, DateTime? To, HashSet<string> Statuses
    );

    public sealed record PagedResult<T>(int Page, int PageSize, int Total, IReadOnlyList<T> Items);
}
