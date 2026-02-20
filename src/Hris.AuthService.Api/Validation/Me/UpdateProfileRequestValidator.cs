using FluentValidation;
using Hris.AuthService.Api.Controllers;

namespace Hris.AuthService.Api.Validation.Me;

public sealed class UpdateProfileRequestValidator : AbstractValidator<MeController.UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Department).MaximumLength(100);
        RuleFor(x => x.JobTitle).MaximumLength(100);
    }
}