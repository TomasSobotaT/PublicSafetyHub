using FluentValidation;
using PublicSafetyHub.Models.Documents;

namespace PublicSafetyHub.Application.Validators;

public class DocumentValidator : AbstractValidator<Document>
{
    public DocumentValidator()
    {
        RuleFor(document => document.Id).NotEmpty();
        RuleFor(document => document.Title).NotEmpty().MaximumLength(512);
        RuleFor(document => document.Body).NotEmpty();
        RuleFor(document => document.Url).MaximumLength(2048)
            .Must(url => string.IsNullOrWhiteSpace(url) ||
                Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
                !string.IsNullOrWhiteSpace(uri.Host) && string.IsNullOrEmpty(uri.UserInfo))
            .WithMessage("Url must be an absolute HTTP or HTTPS URL without credentials.");
    }
}
