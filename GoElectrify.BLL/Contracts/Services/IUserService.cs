using GoElectrify.BLL.Dto.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IUserService
    {
        Task<UserListPageDto> ListAsync(UserListQueryDto query, CancellationToken ct);
    }
}
