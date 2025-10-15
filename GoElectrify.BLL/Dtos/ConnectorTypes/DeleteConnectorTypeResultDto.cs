namespace GoElectrify.BLL.Dtos.ConnectorTypes
{
    public class DeleteConnectorTypeResultDto
    {
        public int Deleted { get; set; }
        public List<int> DeletedIds { get; set; } = new();
        public List<int> BlockedIds { get; set; } = new();
        public List<int> NotFoundIds { get; set; } = new();
    }
}
