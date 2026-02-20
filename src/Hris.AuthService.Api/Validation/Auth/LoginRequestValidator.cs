using FluentValidation;
using Hris.AuthService.Api.Controllers;

namespace Hris.AuthService.Api.Validation.Auth;

public sealed class LoginRequestValidator : AbstractValidator<AuthController.LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(64);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(128);

        RuleFor(x => x.CompanyCode)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(20)
            .Matches("^[A-Z0-9_]+$")
            .WithMessage("CompanyCode must be uppercase letters/numbers/underscore.");
    }
}