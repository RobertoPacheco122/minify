using Minify.Application.UseCases.Url.RetrieveOriginalUrl;
using Minify.Application.UseCases.Url.ShortenUrl;
using Minify.Communication.Requests.Url;
using Minify.Communication.Responses;
using Minify.Communication.Responses.Url;

namespace Minify.API.Endpoints;

public static class UrlEndpoints
{
    public static IEndpointRouteBuilder MapUrlEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{shortenedCode}",
            async (string shortenedCode, IRetrieveOriginalUrlUseCase useCase, CancellationToken cancellationToken) =>
            {
                var result = await useCase.Execute(shortenedCode, cancellationToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(ResultJson.Failure(result.Errors));
                
                return result.Data.Status switch
                {
                    RetrieveOriginalUrlStatus.Found => Results.Redirect(result.Data.LongUrl, permanent: false),
                    RetrieveOriginalUrlStatus.Expired => Results.StatusCode(StatusCodes.Status410Gone),
                    _ => Results.NotFound()
                };
            });

        app.MapPost("/api/shorten-url",
            async (RequestShortenUrlJson request, IShortenUrlUseCase useCase, CancellationToken cancellationToken) =>
            {
                var result = await useCase.Execute(request, cancellationToken);
                
                if(!result.IsSuccess)
                    return Results.BadRequest(ResultJson.Failure(result.Errors));

                return Results.Created($"/{result.Data.ShortenCode}", result);
            });

        return app;
    }
}
