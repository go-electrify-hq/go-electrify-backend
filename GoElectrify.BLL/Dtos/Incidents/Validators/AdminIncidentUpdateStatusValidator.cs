using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.Incidents.Validators
{
    public class AdminIncidentUpdateStatusValidator : AbstractValidator<AdminIncidentStatusUpdateDto>
    {
        private static readonly string[] Allowed = { "OPEN", "IN_PROGRESS", "RESOLVED", "CLOSED" };

        public AdminIncidentUpdateStatusValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty()
                .Must(s => !string.IsNullOrWhiteSpace(s) && Allowed.Contains(s.Trim().ToUpperInvariant()))
                .WithMessage("Status phải là OPEN/IN_PROGRESS/RESOLVED/CLOSED.");

            When(x => string.Equals(x.Status?.Trim(), "CLOSED", StringComparison.OrdinalIgnoreCase), () =>
            {
                RuleFor(x => x.Note)
                    .NotEmpty().WithMessage("Đóng incident cần ghi chú.")
                    .Must(n => string.IsNullOrWhiteSpace(n) || n.Trim().Length <= 1000)
                    .WithMessage("Note tối đa 1000 ký tự.");
            });

            RuleFor(x => x.Note)
                .Must(n => string.IsNullOrWhiteSpace(n) || n.Trim().Length <= 1000)
                .WithMessage("Note tối đa 1000 ký tự.");
        }
    }
}
