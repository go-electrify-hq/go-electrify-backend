using FluentValidation;

namespace GoElectrify.BLL.Dto.VehicleModels.Validators
{
    public class UpdateVehicleModelDtoValidator : AbstractValidator<UpdateVehicleModelDto>
    {
        public UpdateVehicleModelDtoValidator()
        {
            Include(new CreateVehicleModelValidator());
        }
    }
}
