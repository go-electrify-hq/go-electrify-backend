using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Incidents.Validators
{
    public class IncidentCreateValidator : AbstractValidator<IncidentCreateDto>
    {
        private static readonly string[] AllowedSeverity = ["LOW", "MEDIUM", "HIGH", "CRITICAL"];
        public IncidentCreateValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(4000);
            RuleFor(x => x.Severity)
                .NotEmpty()
                .Must(s => AllowedSeverity.Contains(s.ToUpperInvariant()))
                .WithMessage("Severity must be one of LOW, MEDIUM, HIGH, CRITICAL.");
        }
    }
}
