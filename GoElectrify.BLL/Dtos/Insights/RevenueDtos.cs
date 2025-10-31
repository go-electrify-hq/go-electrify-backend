namespace GoElectrify.BLL.Dto.Insights
{
    public class RevenuePointDto
    {
        public DateTime Bucket { get; set; }
        public decimal Amount { get; set; }
    }

    public class RevenueSeriesDto
    {
        public IReadOnlyList<RevenuePointDto> Series { get; set; } = Array.Empty<RevenuePointDto>();
        public decimal Total { get; set; }
    }
}
