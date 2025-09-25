using FluentValidation;

namespace GoElectrify.BLL.Auth.Validators
{
    public sealed class RequestOtpValidator : AbstractValidator<RequestOtpDto>
    {
        public RequestOtpValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }
}
