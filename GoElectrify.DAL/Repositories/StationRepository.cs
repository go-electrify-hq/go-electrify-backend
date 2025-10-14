using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Dto.Station;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

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
            station.Status = string.IsNullOrWhiteSpace(station.Status) ? "ACTIVE" : station.Status.ToUpper();
            await _context.Stations.AddAsync(station);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Station station)
        {
            station.Status = string.IsNullOrWhiteSpace(station.Status) ? "ACTIVE" : station.Status.ToUpper();
            _context.Stations.Update(station);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Station station)
        {
            _context.Stations.Remove(station);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<IReadOnlyList<StationNearResult>> FindNearestAsync(
        double lat, double lng, double radiusKm, int limit, CancellationToken ct)
        {
            if (limit <= 0 || limit > 100) limit = 20;
            if (radiusKm <= 0) radiusKm = 10;

            // ---- Hằng số & tiền xử lý (ngoài biểu thức LINQ) ----
            const double R = 6371.0;               // Earth radius (km)
            const double DegToRad = Math.PI / 180.0;

            var latRad = lat * DegToRad;
            var lngRad = lng * DegToRad;

            // Prefilter bounding box giúp DB bớt tính toán
            const double kmPerDeg = 111.32;
            var latDelta = radiusKm / kmPerDeg;
            var lngDelta = radiusKm / (kmPerDeg * Math.Max(0.01, Math.Abs(Math.Cos(latRad))));

            // ---- Query dịch được sang SQL Server hoàn toàn ----
            var query =
                from s in _context.Stations.AsNoTracking()
                    // Bounding-box (nhanh)
                where (double)s.Latitude >= lat - latDelta && (double)s.Latitude <= lat + latDelta
                   && (double)s.Longitude >= lng - lngDelta && (double)s.Longitude <= lng + lngDelta

                // Chuyển sang radian
                let sLat = (double)s.Latitude * DegToRad
                let sLng = (double)s.Longitude * DegToRad

                // Haversine với Atan2 (dịch tốt hơn Asin/Min)
                let dLat = sLat - latRad
                let dLng = sLng - lngRad
                let sinDLat = Math.Sin(dLat / 2.0)
                let sinDLng = Math.Sin(dLng / 2.0)
                let a = sinDLat * sinDLat
                        + Math.Cos(latRad) * Math.Cos(sLat) * sinDLng * sinDLng
                let c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a))
                let distKm = R * c

                where distKm <= radiusKm
                orderby distKm
                select new StationNearResult(
                    s.Id,
                    s.Name,
                    s.Address,
                    s.Latitude,
                    s.Longitude,
                    s.Status,
                    Math.Round(distKm, 3)
                );

            return await query.Take(limit).ToListAsync(ct);
        }
    }
}
