using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Users
{
    public class UserListPageDto
    {
        public IReadOnlyList<UserListItemDto> Items { get; set; } = new List<UserListItemDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

    }
}
