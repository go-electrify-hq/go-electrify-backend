namespace GoElectrify.BLL.Dto.Station
{
    public sealed record StationNearResult(
    int Id, string Name, string? Address,
    decimal Latitude, decimal Longitude,
    string Status, double DistanceKm);
}
