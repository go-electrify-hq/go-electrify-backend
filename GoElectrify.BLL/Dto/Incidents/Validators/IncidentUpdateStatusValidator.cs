using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Incidents.Validators
{
    public class IncidentUpdateStatusValidator : AbstractValidator<IncidentUpdateStatusDto>
    {
        private static readonly string[] AllowedStatus = ["OPEN", "IN_PROGRESS", "RESOLVED", "CLOSED"];
        public IncidentUpdateStatusValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty()
                .Must(s => AllowedStatus.Contains(s.ToUpperInvariant()))
                .WithMessage("Status must be one of OPEN, IN_PROGRESS, RESOLVED, CLOSED.");
        }
    }
}
