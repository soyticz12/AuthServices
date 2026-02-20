using FluentValidation;
using Hris.AuthService.Api.Controllers;

namespace Hris.AuthService.Api.Validation.Auth;

public sealed class RefreshRequestValidator : AbstractValidator<AuthController.RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MinimumLength(20);
    }
}