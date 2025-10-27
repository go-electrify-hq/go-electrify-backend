namespace GoElectrify.BLL.Dtos.ChargingSession
{
    public sealed class CompletePaymentRequest
    {
        public int? FinalSoc { get; set; }          // % pin khi kết thúc
        public bool? PreferWallet { get; set; }     // true: ví trước; false/null: gói trước
    }
}
