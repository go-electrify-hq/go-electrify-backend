using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class UserRepository(AppDbContext db) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email, CancellationToken ct)
            => db.Users
                 .Include(u => u.Role)
                 .Include(u => u.Wallet)
                 .FirstOrDefaultAsync(u => u.Email == email, ct);

        public Task AddAsync(User user, CancellationToken ct) => db.Users.AddAsync(user, ct).AsTask();

        public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

        public Task<User?> GetByIdAsync(int id, CancellationToken ct)
        => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        public Task<User?> GetDetailAsync(int id, CancellationToken ct)
            => db.Users.Include(u => u.Role).Include(u => u.Wallet)
                       .FirstOrDefaultAsync(u => u.Id == id, ct);
    }
}
