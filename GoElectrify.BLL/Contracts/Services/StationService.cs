using GoElectrify.BLL.Dto;
using GoElectrify.BLL.Dto.Station;
using GoElectrify.BLL.Entities;
using GoElectrify.BLL.Contracts;
using GoElectrify.BLL.Contracts.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace GoElectrify.BLL.Contracts.Services
{
    public class StationService : IStationService
    {
        private readonly IStationRepository _repo;

        public StationService(IStationRepository repo)
        {
            _repo = repo;
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
            var station = new Station
            {
                Name = request.Name,
                Description = request.Description,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Status = "Active",
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
            station.Latitude = request.Latitude ?? station.Latitude;
            station.Longitude = request.Longitude ?? station.Longitude;
            station.Status = request.Status ?? station.Status;
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
    }
}
