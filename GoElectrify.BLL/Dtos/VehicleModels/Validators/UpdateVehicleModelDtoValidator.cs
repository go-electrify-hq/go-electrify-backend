using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
