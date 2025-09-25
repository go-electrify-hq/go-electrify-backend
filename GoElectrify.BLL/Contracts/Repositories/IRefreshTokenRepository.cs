using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken token, CancellationToken ct);

        /// <summary>Lấy refresh token còn hiệu lực theo user + hash.</summary>
        Task<RefreshToken?> FindActiveAsync(int userId, string tokenHash, CancellationToken ct);

        Task SaveAsync(CancellationToken ct);
    }
}
