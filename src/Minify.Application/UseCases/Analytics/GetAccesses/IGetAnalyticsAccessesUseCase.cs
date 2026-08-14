using Minify.Communication.Responses;
using Minify.Communication.Responses.Analytics;

namespace Minify.Application.UseCases.Analytics.GetAccesses;

public interface IGetAnalyticsAccessesUseCase
{
    Task<ResultJson<ResponseAnalyticsAccessesJson>> Execute(
        string shortCode,
        DateTime from,
        DateTime to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
