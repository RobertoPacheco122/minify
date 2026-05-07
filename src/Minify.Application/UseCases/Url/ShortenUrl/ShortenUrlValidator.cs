using FluentValidation;
using Minify.Communication.Requests.Url;

namespace Minify.Application.UseCases.Url.ShortenUrl;

public class ShortenUrlValidator : AbstractValidator<RequestShortenUrlJson>
{
    public ShortenUrlValidator()
    {
        RuleFor(request => request.Url)
            .NotEmpty().WithMessage("Property 'url' is required.")
            .Must(BeAnAbsoluteHttpUrl).WithMessage("Property 'url' must be a valid absolute http or https URL.");
        
        RuleFor(request => request.ExpiresAt)
            .Must(date => date > DateTime.UtcNow).WithMessage("Property 'expiresAt' must be a date in the future.");
    }
    
    private static bool BeAnAbsoluteHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            return false;

        return parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps;
    }
}
