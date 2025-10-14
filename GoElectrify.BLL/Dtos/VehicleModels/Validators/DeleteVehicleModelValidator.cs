using FluentValidation;

namespace GoElectrify.BLL.Dtos.VehicleModels.Validators
{
    public class DeleteVehicleModelValidator : AbstractValidator<DeleteVehicleModelDto>
    {
        public DeleteVehicleModelValidator()
        {
            // Danh sách Id không được null hoặc rỗng
            RuleFor(x => x.Ids)
               .NotNull()
               .WithMessage("The Id list cannot be empty")
               .Must(ids => ids != null && ids.Count > 0)
               .WithMessage("At least one Id is required to delete");

            // Mỗi Id phải > 0 (tránh trường hợp gửi id âm hoặc 0)
            RuleForEach(x => x.Ids)
                .GreaterThan(0)
                .WithMessage("Each ID must be greater than 0");
        }
    }
}
