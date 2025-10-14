using FluentValidation;

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
