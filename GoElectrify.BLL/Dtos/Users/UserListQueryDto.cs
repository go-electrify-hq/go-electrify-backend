namespace GoElectrify.BLL.Dto.Users
{
    public class UserListQueryDto
    {
        public string? Role { get; set; }
        public string? Search { get; set; }

        public string? Sort { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
