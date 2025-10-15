namespace GoElectrify.BLL.Entities
{
    public class ExternalLogin : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public string Provider { get; set; } = default!;       // "GOOGLE"
        public string ProviderUserId { get; set; } = default!; // Google "sub"
    }
}
