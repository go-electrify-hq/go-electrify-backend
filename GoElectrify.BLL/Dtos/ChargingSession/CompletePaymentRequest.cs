namespace GoElectrify.BLL.Dtos.ChargingSession
{
    public sealed class CompletePaymentRequest
    {
        public int? FinalSoc { get; set; }
        public string? Method { get; set; }      // "WALLET" | "SUBSCRIPTION" | null
    }
}
