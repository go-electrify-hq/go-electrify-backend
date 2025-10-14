namespace GoElectrify.BLL.Dto.ConnectorTypes
{
    public class CreateConnectorTypeDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxPowerKw { get; set; }
    }
}
