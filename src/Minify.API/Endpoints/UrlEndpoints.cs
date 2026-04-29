using Minify.Application.UseCases.Url.ShortenUrl;
using Minify.Communication.Requests.Url;

namespace Minify.API.Endpoints;

public static class UrlEndpoints
{
    public static IEndpointRouteBuilder MapUrlEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/shorten-url",
            async (RequestShortenUrlJson request, IShortenUrlUseCase useCase, CancellationToken cancellationToken) =>
            {
                var result = await useCase.Execute(request, cancellationToken);

                return Results.Created($"/{result.ShortenCode}", result);
            });

        return app;
    }
}
