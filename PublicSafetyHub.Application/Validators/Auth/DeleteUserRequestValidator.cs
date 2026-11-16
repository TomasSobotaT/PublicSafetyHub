using FluentValidation;
using PublicSafetyHub.Models.Auth.Requests;

namespace PublicSafetyHub.Application.Validators.Auth;

public class DeleteUserRequestValidator : AbstractValidator<DeleteUserRequest>
{
    public DeleteUserRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(deleteUserRequest => deleteUserRequest.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");
    }
}
