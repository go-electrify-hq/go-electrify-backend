using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public sealed class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly AppDbContext _db;
        public SubscriptionRepository(AppDbContext db) => _db = db;

        public Task<List<Subscription>> GetAllAsync(CancellationToken ct) =>
            _db.Subscriptions.AsNoTracking().OrderBy(s => s.Price).ToListAsync(ct);

        public Task<Subscription?> GetByIdAsync(int id, CancellationToken ct) =>
            _db.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);

        public async Task AddAsync(Subscription sub, CancellationToken ct)
        {
            _db.Subscriptions.Add(sub);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Subscription sub, CancellationToken ct)
        {
            _db.Subscriptions.Update(sub);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        {
            var s = await _db.Subscriptions.FindAsync(new object[] { id }, ct);
            if (s is null) return false;
            _db.Subscriptions.Remove(s);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken ct) =>
            _db.Subscriptions.AnyAsync(s => s.Name == name && (exceptId == null || s.Id != exceptId.Value), ct);
    }
}
