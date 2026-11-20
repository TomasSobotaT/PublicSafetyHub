using FluentValidation;
using PublicSafetyHub.Base.Extensions.Filtering;

namespace PublicSafetyHub.Application.Validators;

public class PaginationRequestValidator : AbstractValidator<PaginationRequest>
{
    public PaginationRequestValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be at least 1.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(request => request.PageNumber)
            .Must((request, pageNumber) => ((long)pageNumber - 1) * request.PageSize <= int.MaxValue)
            .WithMessage("The requested page exceeds the maximum supported offset.");
    }
}
