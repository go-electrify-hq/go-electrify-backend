using System.ComponentModel.DataAnnotations;

namespace GoElectrify.BLL.Dtos.WalletTopup;

public class TopupRequestDto
{
    [Range(10000, double.MaxValue, ErrorMessage = "Amount must be greater than or equal to 10.000")]
    public decimal Amount { get; set; }
    public string locale { get; set; } = "vi"; // vi, en
}
