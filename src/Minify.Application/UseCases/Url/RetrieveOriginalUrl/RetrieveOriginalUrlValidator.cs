using FluentValidation;

namespace Minify.Application.UseCases.Url.RetrieveOriginalUrl;

public class RetrieveOriginalUrlValidator : AbstractValidator<string>
{
    public RetrieveOriginalUrlValidator()
    {
        RuleFor(shortCode => shortCode)
            .NotEmpty().WithMessage("Short code is required.")
            .Length(4, 7).WithMessage("Short code must be between 4 and 7 characters.")
            .Matches("^[a-zA-Z0-9]+$").WithMessage("Short code must contain only alphanumeric characters.");
    }
}
