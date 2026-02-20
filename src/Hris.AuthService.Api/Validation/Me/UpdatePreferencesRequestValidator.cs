using FluentValidation;
using Hris.AuthService.Api.Controllers;

namespace Hris.AuthService.Api.Validation.Me;

public sealed class UpdatePreferencesRequestValidator : AbstractValidator<MeController.UpdatePreferencesRequest>
{
    public UpdatePreferencesRequestValidator()
    {
        RuleFor(x => x.PrefsJson)
            .NotNull()
            .MaximumLength(10000);
    }
}