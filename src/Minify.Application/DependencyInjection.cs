using Microsoft.Extensions.DependencyInjection;
using Minify.Application.UseCases.Analytics.GetAccesses;
using Minify.Application.UseCases.Analytics.GetSummary;
using Minify.Application.UseCases.Url.RetrieveOriginalUrl;
using Minify.Application.UseCases.Url.ShortenUrl;

namespace Minify.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        AddUseCases(services);

        return services;
    }

    private static void AddUseCases(IServiceCollection services)
    {
        services.AddScoped<IShortenUrlUseCase, ShortenUrlUseCase>();
        services.AddScoped<IRetrieveOriginalUrlUseCase, RetrieveOriginalUrlUseCase>();
        services.AddScoped<IGetAnalyticsSummaryUseCase, GetAnalyticsSummaryUseCase>();
        services.AddScoped<IGetAnalyticsAccessesUseCase, GetAnalyticsAccessesUseCase>();
    }
}
