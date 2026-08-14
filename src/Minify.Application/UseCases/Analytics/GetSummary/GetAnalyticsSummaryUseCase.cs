using Minify.Communication.Responses;
using Minify.Communication.Responses.Analytics;
using Minify.Domain.Repositories.UrlAccessEvent;

namespace Minify.Application.UseCases.Analytics.GetSummary;

public class GetAnalyticsSummaryUseCase(IUrlAccessEventReadOnlyRepository repository) : IGetAnalyticsSummaryUseCase
{
    public async Task<ResultJson<ResponseAnalyticsSummaryJson>> Execute(
        string shortCode,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        var validationResult = Validate<ResponseAnalyticsSummaryJson>(shortCode, from, to);
        if (!validationResult.IsSuccess)
            return validationResult;

        var events = await repository.GetAll(shortCode, from, to, cancellationToken);

        var summary = new ResponseAnalyticsSummaryJson
        {
            TotalClicks = events.Count,
            ByCountry = events
                .Where(e => e.Country is not null)
                .GroupBy(e => e.Country!)
                .Select(g => new CountryClicksJson { Country = g.Key, Clicks = g.Count() })
                .OrderByDescending(x => x.Clicks)
                .ToList(),
            ByDevice = events
                .GroupBy(e => e.DeviceType)
                .Select(g => new DeviceClicksJson { DeviceType = g.Key, Clicks = g.Count() })
                .OrderByDescending(x => x.Clicks)
                .ToList(),
            ByBrowser = events
                .GroupBy(e => e.Browser)
                .Select(g => new BrowserClicksJson { Browser = g.Key, Clicks = g.Count() })
                .OrderByDescending(x => x.Clicks)
                .ToList(),
            BySource = events
                .GroupBy(e => e.TrafficSource)
                .Select(g => new SourceClicksJson { Source = g.Key, Clicks = g.Count() })
                .OrderByDescending(x => x.Clicks)
                .ToList(),
            ByDay = events
                .GroupBy(e => e.AccessedAt.Date)
                .Select(g => new DayClicksJson { Date = g.Key.ToString("yyyy-MM-dd"), Clicks = g.Count() })
                .OrderBy(x => x.Date)
                .ToList()
        };

        return ResultJson<ResponseAnalyticsSummaryJson>.Success(summary);
    }

    private static ResultJson<T> Validate<T>(string shortCode, DateTime from, DateTime to)
    {
        var input = new AnalyticsSummaryInput(shortCode, from, to);
        var result = new GetAnalyticsSummaryValidator().Validate(input);

        if (result.IsValid)
            return ResultJson<T>.Success(default!);

        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        return ResultJson<T>.Failure(errors);
    }
}
