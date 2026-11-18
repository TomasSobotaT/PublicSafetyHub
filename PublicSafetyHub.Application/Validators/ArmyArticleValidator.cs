using FluentValidation;
using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Application.Validators;

public class ArmyArticleValidator : AbstractValidator<ArmyArticle>
{
    public ArmyArticleValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(article => article.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(400).WithMessage("Title must not exceed 400 characters.");

        RuleFor(article => article.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(article => article.OriginUrl)
            .NotEmpty().WithMessage("OriginUrl is required.")
            .MaximumLength(2048).WithMessage("OriginUrl must not exceed 2048 characters.")
            .Must(originUrl => Uri.TryCreate(originUrl, UriKind.Absolute, out _))
            .WithMessage("OriginUrl must be an absolute URL.");
    }
}
