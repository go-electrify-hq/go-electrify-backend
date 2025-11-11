using System.Security.Claims;
using GoElectrify.BLL.Dto.Auth;
using Microsoft.AspNetCore.Authentication;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IAuthService
    {
        Task RequestOtpAsync(string email, CancellationToken ct);
        Task<TokenResponse> VerifyOtpAsync(string email, string otp, CancellationToken ct);
        //Task LogoutAsync(int userId, string refreshToken, CancellationToken ct);
        Task LogoutAsync(int userId, string refreshToken, CancellationToken ct);
        Task<TokenResponse> RefreshAsync(string refreshToken, CancellationToken ct);
        (string Scheme, AuthenticationProperties Props) GetGoogleChallenge(string callbackUrl);
        Task<TokenResponse> SignInWithGoogleAsync(ClaimsPrincipal googlePrincipal, CancellationToken ct);
        Task RevokeRefreshTokenAsync(string rawRefreshToken, CancellationToken ct);
    }
}
