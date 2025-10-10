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
        Task UpdateProfileAsync(int userId, string? fullName, string? avatarUrl, CancellationToken ct);
    }
}
