using FluentValidation;
using GoElectrify.BLL.Dto.Auth;

namespace GoElectrify.BLL.Dto.Auth.Validators
{
    public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpDto>
    {
        public VerifyOtpValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Otp).NotEmpty().Length(6);
        }
    }
}
