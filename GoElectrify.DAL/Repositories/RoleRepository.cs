using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class RoleRepository(AppDbContext db) : IRoleRepository
    {
        public Task<Role> GetByNameAsync(string name, CancellationToken ct)
            => db.Roles.FirstAsync(r => r.Name == name, ct);
    }
}
