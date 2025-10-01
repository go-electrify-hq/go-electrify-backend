using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Dto.Charger;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IChargerService
    {
        Task<IReadOnlyList<ChargerDto>> GetAllAsync(CancellationToken ct);
        Task<ChargerDto?> GetByIdAsync(int id, CancellationToken ct);
        Task<ChargerDto> CreateAsync(ChargerCreateDto dto, CancellationToken ct);
        Task<ChargerDto?> UpdateAsync(int id, ChargerUpdateDto dto, CancellationToken ct);
        Task<bool> DeleteAsync(int id, CancellationToken ct);
    }
}
