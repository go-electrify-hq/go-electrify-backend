using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.ConnectorTypes.Validators
{
    public class UpdateConnectorTypeDtoValidator : AbstractValidator<UpdateConnectorTypeDto>
    {
        public UpdateConnectorTypeDtoValidator() 
        {
            Include(new CreateConnectorTypeDtoValidator());
        }
    }
}
