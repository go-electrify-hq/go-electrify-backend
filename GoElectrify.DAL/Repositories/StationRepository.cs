using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;
using GoElectrify.BLL.Contracts.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.DAL.Repositories
{
    public class StationRepository : IStationRepository
    {
        private readonly AppDbContext _context;

        public StationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Station>> GetAllAsync()
        {
            return await _context.Stations.ToListAsync();
        }

        public async Task<Station?> GetByIdAsync(int id)
        {
            return await _context.Stations.FindAsync(id);
        }

        public async Task AddAsync(Station station)
        {
            await _context.Stations.AddAsync(station);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Station station)
        {
            _context.Stations.Update(station);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Station station)
        {
            _context.Stations.Remove(station);
            await _context.SaveChangesAsync();
        }
    }
}
