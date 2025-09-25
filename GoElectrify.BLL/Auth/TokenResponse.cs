namespace GoElectrify.BLL.Auth
{
    public sealed record TokenResponse(
            string AccessToken,
            DateTime AccessExpires,
            string RefreshToken,
            DateTime RefreshExpires);
}
