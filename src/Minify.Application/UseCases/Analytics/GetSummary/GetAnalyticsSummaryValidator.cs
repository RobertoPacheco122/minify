using FluentValidation;

namespace Minify.Application.UseCases.Analytics.GetSummary;

public record AnalyticsSummaryInput(string ShortCode, DateTime From, DateTime To);

public class GetAnalyticsSummaryValidator : AbstractValidator<AnalyticsSummaryInput>
{
    public GetAnalyticsSummaryValidator()
    {
        RuleFor(x => x.ShortCode)
            .NotEmpty().WithMessage("Short code is required.");

        RuleFor(x => x.To)
            .GreaterThan(x => x.From).WithMessage("'to' must be after 'from'.");
    }
}
