using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.ConnectorTypes.Validators
{
    public class CreateConnectorTypeDtoValidator : AbstractValidator<CreateConnectorTypeDto>
    {
        public CreateConnectorTypeDtoValidator() 
        {
            RuleFor(x => x.Name)
               .NotEmpty().WithMessage("Name is required.")
               .MinimumLength(2).WithMessage("Name must be at least 2 characters.")
               .MaximumLength(50).WithMessage("Name must be at most 50 characters.")
               .Must(n => n == n?.Trim()).WithMessage("Name should not start or end with spaces.");

            RuleFor(x => x.Description)
                .MaximumLength(300).When(x => !string.IsNullOrWhiteSpace(x.Description))
                .WithMessage("Description is too long.");

            RuleFor(x => x.MaxPowerKw)
                .GreaterThan(0).WithMessage("MaxPowerKw must be > 0.")
                .LessThanOrEqualTo(1000).WithMessage("MaxPowerKw is unrealistically high.");
        }
    }
}
