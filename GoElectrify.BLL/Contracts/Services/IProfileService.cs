namespace GoElectrify.BLL.Contracts.Services
{
    public interface IProfileService
    {
        Task<object> GetMeAsync(int userId, CancellationToken ct);
        Task UpdateProfileAsync(int userId, string? fullName, string? avatarUrl, CancellationToken ct);
        Task UpdateAvatarAsync(int userId, string? avatarUrl, CancellationToken ct);
        Task UpdateFullNameAsync(int userId, string? fullName, CancellationToken ct);
    }
}
