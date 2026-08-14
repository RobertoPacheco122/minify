using FluentValidation;

namespace Minify.Application.UseCases.Analytics.GetAccesses;

public record AnalyticsAccessesInput(string ShortCode, DateTime From, DateTime To, int Page, int PageSize);

public class GetAnalyticsAccessesValidator : AbstractValidator<AnalyticsAccessesInput>
{
    public GetAnalyticsAccessesValidator()
    {
        RuleFor(x => x.ShortCode)
            .NotEmpty().WithMessage("Short code is required.");

        RuleFor(x => x.To)
            .GreaterThan(x => x.From).WithMessage("'to' must be after 'from'.");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("'page' must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200).WithMessage("'pageSize' must be between 1 and 200.");
    }
}
