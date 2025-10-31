namespace GoElectrify.BLL.Dto.Insights
{
    public class UsagePointDto
    {
        public DateTime Bucket { get; set; }
        public int Count { get; set; }
    }

    public class UsageSeriesDto
    {
        public IReadOnlyList<UsagePointDto> Series { get; set; } = Array.Empty<UsagePointDto>();
        public int PeakHour { get; set; }        // 0..23
        public int PeakHourCount { get; set; }
        public int TotalSessions { get; set; }
    }
}
