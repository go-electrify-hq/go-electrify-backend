using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Users;

namespace GoElectrify.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _users;

        public UserService(IUserRepository users)
        {
            _users = users;
        }

        public async Task<UserListPageDto> ListAsync(UserListQueryDto query, CancellationToken ct)
        {
            // Chuẩn hóa page/pageSize
            int page = query.Page;
            if (page <= 0) page = 1;

            int pageSize = query.PageSize;
            if (pageSize <= 0) pageSize = 20;
            if (pageSize > 200) pageSize = 200;

            // Lấy dữ liệu từ repo
            var repoResult = await _users.ListAsync(
                query.Role, query.Search, query.Sort, page, pageSize, ct);

            // Map sang DTO cho UI
            var list = new List<UserListItemDto>();
            foreach (var u in repoResult.Items)
            {
                var dto = new UserListItemDto();
                dto.Id = u.Id;
                dto.Email = u.Email;
                dto.FullName = u.FullName;

                string roleName = string.Empty;
                if (u.Role != null)
                {
                    roleName = u.Role.Name;
                }
                dto.Role = roleName;

                decimal bal = 0m;
                if (u.Wallet != null)
                {
                    bal = u.Wallet.Balance;
                }
                dto.WalletBalance = bal;

                dto.CreateAt = u.CreatedAt;

                list.Add(dto);
            }

            var pageDto = new UserListPageDto();
            pageDto.Items = list;
            pageDto.Total = repoResult.Total;
            pageDto.Page = page;
            pageDto.PageSize = pageSize;

            return pageDto;
        }
    }
}
