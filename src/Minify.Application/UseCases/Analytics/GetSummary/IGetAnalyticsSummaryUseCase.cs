using Minify.Communication.Responses;
using Minify.Communication.Responses.Analytics;

namespace Minify.Application.UseCases.Analytics.GetSummary;

public interface IGetAnalyticsSummaryUseCase
{
    Task<ResultJson<ResponseAnalyticsSummaryJson>> Execute(
        string shortCode,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
