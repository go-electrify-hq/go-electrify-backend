namespace GoElectrify.BLL.Contracts.Services
{
    public interface IBookingFeeService
    {
        Task<(string type, decimal value)> GetAsync(CancellationToken ct);
    }
}
