namespace GoElectrify.DAL.Infra
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public string Secret { get; set; } = default!;
        public int AccessMinutes { get; set; } = 30;
        public int RefreshDays { get; set; } = 30;
    }
}
