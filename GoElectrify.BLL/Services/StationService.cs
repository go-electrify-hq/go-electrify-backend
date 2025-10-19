using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Station;
using GoElectrify.BLL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Dto.Charger;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace GoElectrify.BLL.Services
{
    public class StationService : IStationService
    {
        private readonly IStationRepository _repo;
        private readonly IStationStaffRepository _staffRepo;
        private readonly IWebHostEnvironment _env;
        public StationService(IStationRepository repo, IStationStaffRepository staffRepo, IWebHostEnvironment env)
        {
            _repo = repo;
            _staffRepo = staffRepo;
            _env = env;
        }

        public async Task<IEnumerable<Station>> GetAllStationsAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<Station?> GetStationByIdAsync(int id)
        {
            return await _repo.GetByIdAsync(id);
        }



        public async Task<Station> CreateStationAsync(StationCreateDto request)
        {
            var status = (request.Status ?? "ACTIVE").ToUpperInvariant();

            var station = new Station
            {
                Name = request.Name,
                Description = request.Description,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(station);
            return station;
        }

        public async Task<Station?> UpdateStationAsync(int id, StationUpdateDto request)
        {
            var station = await _repo.GetByIdAsync(id);
            if (station == null) return null;

            station.Name = request.Name ?? station.Name;
            station.Description = request.Description ?? station.Description;
            station.Address = request.Address ?? station.Address;
            station.ImageUrl = request.ImageUrl ?? station.ImageUrl;
            station.Latitude = request.Latitude ?? station.Latitude;
            station.Longitude = request.Longitude ?? station.Longitude;
            station.Status = request.Status ?? station.Status;
            if (!string.IsNullOrWhiteSpace(request.Status))
                station.Status = request.Status!.ToUpperInvariant();
            station.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(station);
            return station;
        }

        public async Task<bool> DeleteStationAsync(int id)
        {
            var station = await _repo.GetByIdAsync(id);
            if (station == null) return false;

            await _repo.DeleteAsync(station);
            return true;
        }

        public async Task<IReadOnlyList<StationNearbyDto>> GetNearbyAsync(
        double lat, double lng, double radiusKm = 10, int limit = 20, CancellationToken ct = default)
        {
            if (lat is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(lat));
            if (lng is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(lng));

            var rows = await _repo.FindNearestAsync(lat, lng, radiusKm, limit, ct);
            return rows.Select(r => new StationNearbyDto
            {
                Id = r.Id,
                Name = r.Name,
                Address = r.Address,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Status = r.Status,
                DistanceKm = r.DistanceKm
            }).ToList();
        }


        public async Task<Station?> GetMyStationAsync(int userId, CancellationToken ct)
        {
            var assignment = await _staffRepo.GetActiveByUserIdAsync(userId, ct);
            if (assignment == null) return null;
            return assignment.Station;

        }

        public async Task<string> UploadStationImageAsync(int stationId, IFormFile file, string baseUrl)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file uploaded.");

            var station = await _repo.GetByIdAsync(stationId);
            if (station == null)
                throw new KeyNotFoundException("Station not found.");

            // Tạo folder nếu chưa có
            var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "stations");
            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // Tạo tên file duy nhất
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadPath, fileName);

            // Lưu file vật lý
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Cập nhật URL ảnh trong DB
            var imageUrl = $"{baseUrl}/uploads/stations/{fileName}";
            station.ImageUrl = imageUrl;
            await _repo.UpdateAsync(station);
            await _repo.SaveChangesAsync();

            return imageUrl;
        }
        public Task<bool> ExistsAsync(int id, CancellationToken ct)
        => _repo.ExistsAsync(id, ct);
    }
}
