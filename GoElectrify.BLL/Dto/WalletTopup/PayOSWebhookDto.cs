namespace GoElectrify.BLL.DTOs.WalletTopup;

public class PayOSWebhookDto
{
    /// <summary>"00" = success</summary>
    public string Code { get; set; } = string.Empty;
    public WebhookDataDto Data { get; set; } = new();
    public string Signature { get; set; } = string.Empty;
}

public class WebhookDataDto
{
    public long orderCode { get; set; } 
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
