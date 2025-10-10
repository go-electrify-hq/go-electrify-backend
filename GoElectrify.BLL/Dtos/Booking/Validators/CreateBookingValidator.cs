using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace GoElectrify.BLL.Dto.Booking.Validators
{
    public sealed class CreateBookingValidator : AbstractValidator<CreateBookingDto>
    {
        public CreateBookingValidator()
        {
            RuleFor(x => x.StationId).GreaterThan(0);
            RuleFor(x => x.ConnectorTypeId).GreaterThan(0);
            RuleFor(x => x.VehicleModelId).GreaterThan(0);
            RuleFor(x => x.InitialSoc).InclusiveBetween(0, 100);
            RuleFor(x => x.ScheduledStart)
                .Must(dt => dt > DateTime.UtcNow.AddMinutes(5))
                .WithMessage("ScheduledStart must be at least +5 minutes in the future (UTC).");
        }
    }
}
