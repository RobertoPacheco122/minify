using Minify.Communication.Responses;
using Minify.Communication.Responses.Analytics;
using Minify.Domain.Repositories.UrlAccessEvent;

namespace Minify.Application.UseCases.Analytics.GetAccesses;

public class GetAnalyticsAccessesUseCase(IUrlAccessEventReadOnlyRepository repository) : IGetAnalyticsAccessesUseCase
{
    public async Task<ResultJson<ResponseAnalyticsAccessesJson>> Execute(
        string shortCode,
        DateTime from,
        DateTime to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var validationResult = Validate<ResponseAnalyticsAccessesJson>(shortCode, from, to, page, pageSize);
        if (!validationResult.IsSuccess)
            return validationResult;

        var (items, total) = await repository.GetPaged(shortCode, from, to, page, pageSize, cancellationToken);

        return ResultJson<ResponseAnalyticsAccessesJson>.Success(new ResponseAnalyticsAccessesJson
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(e => new AccessEventItemJson
            {
                AccessedAt = e.AccessedAt,
                Country = e.Country,
                City = e.City,
                DeviceType = e.DeviceType,
                Browser = e.Browser,
                OperatingSystem = e.OperatingSystem,
                TrafficSource = e.TrafficSource,
                UtmSource = e.UtmSource,
                UtmMedium = e.UtmMedium,
                UtmCampaign = e.UtmCampaign
            }).ToList()
        });
    }

    private static ResultJson<T> Validate<T>(string shortCode, DateTime from, DateTime to, int page, int pageSize)
    {
        var input = new AnalyticsAccessesInput(shortCode, from, to, page, pageSize);
        var result = new GetAnalyticsAccessesValidator().Validate(input);

        if (result.IsValid)
            return ResultJson<T>.Success(default!);

        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        return ResultJson<T>.Failure(errors);
    }
}
