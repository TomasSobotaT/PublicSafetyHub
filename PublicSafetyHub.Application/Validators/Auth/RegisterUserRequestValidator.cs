using FluentValidation;
using PublicSafetyHub.Models.Auth.Requests;

namespace PublicSafetyHub.Application.Validators.Auth;

public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(registerUserRequest => registerUserRequest.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(registerUserRequest => registerUserRequest.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(12).WithMessage("Password must contain at least 12 characters.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");

        RuleFor(registerUserRequest => registerUserRequest.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required.")
            .MaximumLength(128).WithMessage("Password confirmation must not exceed 128 characters.")
            .Equal(registerUserRequest => registerUserRequest.Password, StringComparer.Ordinal).WithMessage("Passwords must match.");
    }
}
