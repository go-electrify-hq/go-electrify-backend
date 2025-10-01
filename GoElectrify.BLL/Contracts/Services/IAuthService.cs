using GoElectrify.BLL.Dto.Auth;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IAuthService
    {
        Task RequestOtpAsync(string email, CancellationToken ct);
        Task<TokenResponse> VerifyOtpAsync(string email, string otp, CancellationToken ct);
        //Task LogoutAsync(int userId, string refreshToken, CancellationToken ct);
        Task LogoutAsync(int userId, string refreshToken, CancellationToken ct);
        Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct);
    }
}
