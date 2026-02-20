using FluentValidation;
using Hris.AuthService.Api.Controllers;

namespace Hris.AuthService.Api.Validation.Admin;

public sealed class CreateUserRequestValidator : AbstractValidator<AdminUsersController.CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(64);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(128);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Roles)
            .NotNull()
            .Must(r => r.Length > 0)
            .WithMessage("At least one role is required.");

        RuleForEach(x => x.Roles)
            .NotEmpty()
            .MaximumLength(50);
    }
}