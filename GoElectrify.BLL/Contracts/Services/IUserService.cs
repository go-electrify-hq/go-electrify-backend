using GoElectrify.BLL.Dto.Users;
using GoElectrify.BLL.Dtos.Users;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IUserService
    {
        Task<UserListPageDto> ListAsync(UserListQueryDto query, CancellationToken ct);
        Task<UserListPageDto> ListAsync(UserListQueryDto query, CancellationToken ct);

        Task<UserRoleChangedDto> UpdateRoleAsync(
            int actingUserId,
            int targetUserId,
            string roleName,
            bool forceSignOut,
            CancellationToken ct);
    }
}
