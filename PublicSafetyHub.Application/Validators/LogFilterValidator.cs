using FluentValidation;
using PublicSafetyHub.Models.Logging;

namespace PublicSafetyHub.Application.Validators;

public class LogFilterValidator : AbstractValidator<LogFilter>
{
    public LogFilterValidator()
    {
        RuleFor(filter => filter.Level).IsInEnum();
        RuleFor(filter => filter.From).Must(value => !value.HasValue || value.Value.Kind == DateTimeKind.Utc)
            .WithMessage("From must be a UTC timestamp with a Z suffix.");
        RuleFor(filter => filter.To).Must(value => !value.HasValue || value.Value.Kind == DateTimeKind.Utc)
            .WithMessage("To must be a UTC timestamp with a Z suffix.");
        RuleFor(filter => filter).Must(filter => !filter.From.HasValue || !filter.To.HasValue || filter.From.Value <= filter.To.Value)
            .WithMessage("From must not be later than To.");
        RuleFor(filter => filter.Category).MaximumLength(512);
        RuleFor(filter => filter.SearchValue).MaximumLength(2048);
        RuleFor(filter => filter.TraceId).MaximumLength(128);
    }
}
