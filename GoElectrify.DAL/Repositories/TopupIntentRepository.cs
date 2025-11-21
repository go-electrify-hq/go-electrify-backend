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


    /*
    public async Task<string?> GetUserEmailByWalletAsync(int walletId)
    {
        return await (from w in _db.Wallets.AsNoTracking()
                      join u in _db.Users.AsNoTracking() on w.UserId equals u.Id
                      where w.Id == walletId
                      select u.Email)
                     .FirstOrDefaultAsync();
    }*/
}
