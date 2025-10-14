namespace GoElectrify.DockConsole.Contracts
{
    public record DockConsoleOptions
    {
        public string ApiBase { get; init; } = "http://localhost:5022";
        public Dictionary<string, string> Docks { get; init; } = new();
    }
}
