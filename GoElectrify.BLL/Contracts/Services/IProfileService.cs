using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IProfileService
    {
        Task<object> GetMeAsync(int userId, CancellationToken ct);
        Task UpdateFullNameAsync(int userId, string? fullName, CancellationToken ct);
    }
}
