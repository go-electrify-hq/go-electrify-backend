using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface ISystemSettingRepository
    {
        Task<string?> GetAsync(string key, CancellationToken ct);
        Task UpsertAsync(string key, string value, int? updatedBy, CancellationToken ct);
    }
}
