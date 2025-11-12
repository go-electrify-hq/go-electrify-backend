using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Users;
using GoElectrify.BLL.Dtos.Users;
using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _users;
        private readonly IRoleRepository _roles;
        private readonly IRefreshTokenRepository _refreshTokens;

        public UserService(
            IUserRepository users,
            IRoleRepository roles,
            IRefreshTokenRepository refreshTokens)
        {
            _users = users;
            _roles = roles;
            _refreshTokens = refreshTokens;
        }

        public async Task<UserListPageDto> ListAsync(UserListQueryDto query, CancellationToken ct)
        {

            // Lấy dữ liệu từ repo
            var repoResult = await _users.ListAsync(
                query.Role, query.Search, query.Sort, query.Page, query.PageSize, ct);

            // Map sang DTO cho UI
            var list = new List<UserListItemDto>();
            foreach (var u in repoResult.Items)
            {
                list.Add(new UserListItemDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role?.Name ?? string.Empty,
                    WalletBalance = u.Wallet?.Balance ?? 0m,
                    CreateAt = u.CreatedAt
                });
            }

            return new UserListPageDto
            {
                Items = list,
                Total = repoResult.Total,  
                Page = 1,                 
                PageSize = list.Count     
            };
        }

        public async Task<UserRoleChangedDto> UpdateRoleAsync(
            int actingUserId,
            int targetUserId,
            string roleName,
            bool forceSignOut,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new InvalidOperationException("invalid_role");

            // Lấy user (tracked + include Role)
            var user = await _users.GetDetailAsync(targetUserId, ct);
            if (user is null)
                throw new InvalidOperationException("user_not_found");

            // Lấy role theo tên (case-sensitive theo repo hiện tại)
            Role role;
            try
            {
                role = await _roles.GetByNameAsync(roleName.Trim(), ct);
            }
            catch
            {
                throw new InvalidOperationException("role_not_found");
            }

            var oldRoleName = user.Role?.Name ?? "Driver";
            var newRoleName = role.Name;

            if (oldRoleName == newRoleName)
            {
                return new UserRoleChangedDto
                {
                    UserId = user.Id,
                    OldRole = oldRoleName,
                    NewRole = newRoleName
                };
            }

            // Không được tự hạ role mình xuống nếu là Admin cuối
            // (và nói chung không cho đổi role của chính mình để tránh tự khóa)
            if (actingUserId == targetUserId)
                throw new InvalidOperationException("cannot_change_own_role");

            // Chặn xóa Admin cuối
            if (oldRoleName == "Admin" && newRoleName != "Admin")
            {
                // Dùng ListAsync(role:"Admin") để lấy Total (CountAsync ở repo)
                var (_, totalAdmins) = await _users.ListAsync("Admin", null, null, 1, 1, ct);
                if (totalAdmins <= 1)
                    throw new InvalidOperationException("cannot_remove_last_admin");
            }

            // Update
            user.RoleId = role.Id;
            user.Role = role;
            await _users.SaveAsync(ct);

            // Revoke refresh tokens để tác dụng nhanh hơn
            if (forceSignOut)
            {
                await _refreshTokens.RevokeAllActiveByUserAsync(user.Id, ct);
            }

            return new UserRoleChangedDto
            {
                UserId = user.Id,
                OldRole = oldRoleName,
                NewRole = newRoleName
            };
        }
    }
}
