using FluentValidation;
using Hris.AuthService.Api.Controllers;

namespace Hris.AuthService.Api.Validation.Me;

public sealed class UpdatePhotoRequestValidator : AbstractValidator<MeController.UpdatePhotoRequest>
{
    public UpdatePhotoRequestValidator()
    {
        RuleFor(x => x.PhotoUrl)
            .NotEmpty()
            .MaximumLength(2000);
    }
}