namespace GoElectrify.BLL.Entities
{
    public class User : BaseEntity
    {
        public int RoleId { get; set; }
        public Role? Role { get; set; }
        public string Email { get; set; } = default!;
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public Wallet? Wallet { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<StationStaff> StationStaff { get; set; } = new List<StationStaff>();

    }
}
