using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories;

public class TopupIntentRepository : ITopupIntentRepository
{
    private readonly AppDbContext _db;
    public TopupIntentRepository(AppDbContext db) => _db = db;

    public async Task<TopupIntent?> GetByProviderRefAsync(long orderCode)
        => await _db.TopupIntents.FirstOrDefaultAsync(t => t.OrderCode == orderCode);

    public async Task<TopupIntent> AddAsync(TopupIntent entity)
    {
        _db.TopupIntents.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(TopupIntent entity)
    {
        _db.TopupIntents.Update(entity);
        await _db.SaveChangesAsync();
    }
}
