namespace GoElectrify.BLL.Dto.StationStaff
{
    public class StationStaffDto
    {
        public int StationId { get; set; }
        public int UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserFullName { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
