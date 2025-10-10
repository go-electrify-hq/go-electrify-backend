using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Station
{
    public sealed record StationNearResult(
    int Id, string Name, string? Address,
    decimal Latitude, decimal Longitude,
    string Status, double DistanceKm);
}
