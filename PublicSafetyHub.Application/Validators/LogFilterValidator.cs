using FluentValidation;
using PublicSafetyHub.Models.Logging;

namespace PublicSafetyHub.Application.Validators;

public class LogFilterValidator : AbstractValidator<LogFilter>
{
    public LogFilterValidator()
    {
        RuleForEach(filter => filter.Levels).IsInEnum();
        RuleFor(filter => filter.DateFrom).Must(value => !value.HasValue || value.Value.Kind == DateTimeKind.Utc)
            .WithMessage("DateFrom must be a UTC timestamp with a Z suffix.");
        RuleFor(filter => filter.DateTo).Must(value => !value.HasValue || value.Value.Kind == DateTimeKind.Utc)
            .WithMessage("DateTo must be a UTC timestamp with a Z suffix.");
        RuleFor(filter => filter).Must(filter => !filter.DateFrom.HasValue || !filter.DateTo.HasValue || filter.DateFrom.Value <= filter.DateTo.Value)
            .WithMessage("DateFrom must not be later than DateTo.");
        RuleFor(filter => filter.Category).MaximumLength(512);
        RuleFor(filter => filter.SearchValue).MaximumLength(2048);
        RuleFor(filter => filter.TraceId).MaximumLength(128);
    }
}
