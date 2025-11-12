using System.ComponentModel.DataAnnotations;

namespace GoElectrify.BLL.Dtos.Wallet
{
    public class ManualDepositRequestDto
    {
        [Required]
        [Range(10000, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn hoặc bằng 10.000")]
        public decimal Amount { get; set; }

        public string? Note { get; set; }

    }
}
