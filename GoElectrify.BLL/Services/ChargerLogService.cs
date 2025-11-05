using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dtos.ChargerLogs;
using GoElectrify.BLL.Dtos.ChargingSession;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Repositories;

namespace GoElectrify.BLL.Services
{
    public sealed class ChargerLogService(IChargerLogRepository repo) : IChargerLogService
    {
        public async Task<PagedResult<ChargerLogItemDto>> GetLogsAsync(
            int chargerId, ChargerLogQueryDto q, CancellationToken ct)
        {
            var page = Math.Max(1, q.Page);
            var pageSize = Math.Clamp(q.PageSize, 1, 200);

            var (total, items) = await repo.GetLogsPagedAsync(
                chargerId, q.From, q.To, q.States, q.ErrorCodes, q.Ascending, page, pageSize, ct);

            var mapped = items.Select(Map).ToList();
            return new PagedResult<ChargerLogItemDto>(page, pageSize, total, mapped);
        }

        private static ChargerLogItemDto Map(ChargerLog e) => new(
            e.Id,
            e.SampleAt,
            e.Voltage,
            e.Current,
            e.PowerKw,
            e.SessionEnergyKwh,
            e.SocPercent,
            e.State,
            e.ErrorCode
        );
    }
}
