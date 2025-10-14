using FluentValidation;

namespace GoElectrify.BLL.Dto.Auth.Validators
{
    public sealed class RequestOtpValidator : AbstractValidator<RequestOtpDto>
    {
        public RequestOtpValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }
}
