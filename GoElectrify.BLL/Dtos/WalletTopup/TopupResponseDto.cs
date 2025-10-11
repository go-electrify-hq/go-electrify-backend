namespace GoElectrify.BLL.Dtos.WalletTopup;

public class TopupResponseDto
{
    public int TopupIntentId { get; set; }
    public string CheckoutUrl { get; set; } = string.Empty;
    public long OrderCode { get; set; }
}
