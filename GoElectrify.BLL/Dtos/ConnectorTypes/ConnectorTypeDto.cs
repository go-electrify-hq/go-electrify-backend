namespace GoElectrify.BLL.Dto.ConnectorTypes
{
    public class ConnectorTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxPowerKw { get; set; }
    }
}
