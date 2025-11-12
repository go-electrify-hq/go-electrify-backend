using FluentValidation;
using GoElectrify.BLL.Dto.Incidents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.Incidents.Validators
{
    public class AdminIncidentListQueryValidator : AbstractValidator<AdminIncidentListQueryDto>
    {
        private static readonly string[] StatusSet = { "OPEN", "IN_PROGRESS", "RESOLVED", "CLOSED" };
        private static readonly string[] SeveritySet = { "LOW", "MEDIUM", "HIGH", "CRITICAL" };

        public AdminIncidentListQueryValidator()
        {
            RuleFor(x => x.Take).InclusiveBetween(1, 200);
            RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);

            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrWhiteSpace(s) || StatusSet.Contains(s.Trim().ToUpperInvariant()))
                .WithMessage("Status phải là OPEN/IN_PROGRESS/RESOLVED/CLOSED.");

            RuleFor(x => x.Severity)
                .Must(s => string.IsNullOrWhiteSpace(s) || SeveritySet.Contains(s.Trim().ToUpperInvariant()))
                .WithMessage("Severity phải là LOW/MEDIUM/HIGH/CRITICAL.");

            RuleFor(x => x.Keyword)
                .Must(k => string.IsNullOrWhiteSpace(k) || k.Trim().Length <= 100)
                .WithMessage("Keyword tối đa 100 ký tự.");

            RuleFor(x => x)
                .Must(x => !(x.FromReportedAt.HasValue && x.ToReportedAt.HasValue)
                           || x.FromReportedAt <= x.ToReportedAt)
                .WithMessage("FromReportedAt phải <= ToReportedAt.");
        }
    }
}
