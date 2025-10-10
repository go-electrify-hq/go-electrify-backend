using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.VehicleModels.Validators
{
    public class CreateVehicleModelValidator : AbstractValidator<CreateVehicleModelDto>
    {
        public CreateVehicleModelValidator() 
        {
            RuleFor(x => x.ModelName)
                .NotEmpty().WithMessage("ModelName is required.")
                .MinimumLength(2).WithMessage("ModelName must be at least 2 characters.")
                .MaximumLength(100).WithMessage("ModelName must be at most 100 characters.")
                .Must(n => n == n?.Trim()).WithMessage("ModelName should not start or end with spaces.");

            RuleFor(x => x.MaxPowerKw)
                .GreaterThan(0).WithMessage("MaxPowerKw must be > 0.")
                .LessThanOrEqualTo(1000).WithMessage("MaxPowerKw is too large.");

            RuleFor(x => x.BatteryCapacityKwh)
                .GreaterThan(0).WithMessage("BatteryCapacityKwh must be > 0.")
                .LessThanOrEqualTo(1000).WithMessage("BatteryCapacityKwh is too large.");

            RuleFor(x => x.ConnectorTypeIds)
                .Must(list => list == null || list.All(id => id > 0))
                .WithMessage("ConnectorTypeIds must contain positive ids only.")
                .Must(list => list == null || list.Distinct().Count() == list.Count)
                .WithMessage("ConnectorTypeIds must be unique.");
        }
    }
}
