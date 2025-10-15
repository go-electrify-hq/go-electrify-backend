using GoElectrify.BLL.Dto.Users;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IUserService
    {
        Task<UserListPageDto> ListAsync(UserListQueryDto query, CancellationToken ct);
    }
}
