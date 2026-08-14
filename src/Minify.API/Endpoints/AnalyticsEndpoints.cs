using Minify.Application.UseCases.Analytics.GetAccesses;
using Minify.Application.UseCases.Analytics.GetSummary;
using Minify.Communication.Responses;

namespace Minify.API.Endpoints;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/analytics/{shortCode}/summary",
            async (string shortCode,
                   DateTime from,
                   DateTime to,
                   IGetAnalyticsSummaryUseCase useCase,
                   CancellationToken cancellationToken) =>
            {
                var result = await useCase.Execute(shortCode, from, to, cancellationToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(ResultJson.Failure(result.Errors));

                return Results.Ok(result.Data);
            });

        app.MapGet("/api/analytics/{shortCode}/accesses",
            async (string shortCode,
                   DateTime from,
                   DateTime to,
                   IGetAnalyticsAccessesUseCase useCase,
                   int page = 1,
                   int pageSize = 50,
                   CancellationToken cancellationToken = default) =>
            {
                var result = await useCase.Execute(shortCode, from, to, page, pageSize, cancellationToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(ResultJson.Failure(result.Errors));

                return Results.Ok(result.Data);
            });

        return app;
    }
}
