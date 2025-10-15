namespace GoElectrify.BLL.Dto.Users
{
    public class UserListItemDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string Role { get; set; } = string.Empty;
        public decimal WalletBalance { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
