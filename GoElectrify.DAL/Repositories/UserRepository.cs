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

        public async Task<(IReadOnlyList<User> Items, int Total)> ListAsync(
            string? role, string? search, string? sort, int page, int pageSize, CancellationToken ct)
        {
            // Base query
            IQueryable<User> q = db.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.Wallet);

            // Filter role
            if (!string.IsNullOrWhiteSpace(role))
            {
                string roleName = role.Trim();
                q = q.Where(u => u.Role != null && u.Role.Name == roleName);
            }
            else
            {
                // Mặc định chỉ Staff + Driver
                q = q.Where(u => u.Role != null &&
                                 (u.Role.Name == "Staff" || u.Role.Name == "Driver"));
            }

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                string kw = search.Trim().ToLower();
                q = q.Where(u => u.Email.ToLower().Contains(kw) ||
                                 (u.FullName != null && u.FullName.ToLower().Contains(kw)));
            }

            // Sort
            string sortKey = "createdAt_desc";
            if (!string.IsNullOrWhiteSpace(sort))
            {
                sortKey = sort.Trim().ToLower();
            }

            if (sortKey == "createdat_asc")
            {
                q = q.OrderBy(u => u.CreatedAt).ThenBy(u => u.Id);
            }
            else if (sortKey == "email_asc")
            {
                q = q.OrderBy(u => u.Email).ThenByDescending(u => u.CreatedAt);
            }
            else if (sortKey == "email_desc")
            {
                q = q.OrderByDescending(u => u.Email).ThenByDescending(u => u.CreatedAt);
            }
            else
            {
                q = q.OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id);
            }

            // Normalize paging
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;
            if (pageSize > 200) pageSize = 200;

            int total = await q.CountAsync(ct);

            int skip = (page - 1) * pageSize;
            if (skip < 0) skip = 0;

            List<User> items = await q
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }

    }
}
