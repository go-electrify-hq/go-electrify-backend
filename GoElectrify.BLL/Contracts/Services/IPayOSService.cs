namespace GoElectrify.BLL.Services.Interfaces
{
    public interface IPayOSService
    {
        Task<(string checkoutUrl, long orderCode)> CreatePaymentLinkAsync(decimal amount, string description, string returnUrl, string cancelUrl);
        bool VerifySignature(Dictionary<string, object> data, string signature);
    }
}
