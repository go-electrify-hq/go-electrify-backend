using System.ComponentModel.DataAnnotations;

namespace GoElectrify.BLL.Dtos.WalletTopup;

public class TopupRequestDto
{
    [Range(typeof(decimal), "10000", "10000000", ErrorMessage = "Amount must be between 10.000 and 10.000.000")]
    public decimal Amount { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}
