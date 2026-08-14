using Minify.Application.UseCases.Url.RetrieveOriginalUrl;
using Minify.Application.UseCases.Url.ShortenUrl;
using Minify.Communication.Requests.Url;
using Minify.Communication.Responses;
using Minify.Communication.Responses.Url;
using Minify.Messaging.Events;
using Minify.Messaging.Publishers;

namespace Minify.API.Endpoints;

public static class UrlEndpoints
{
    public static IEndpointRouteBuilder MapUrlEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{shortenedCode}",
            async (string shortenedCode,
                   HttpContext httpContext,
                   IRetrieveOriginalUrlUseCase useCase,
                   IEventPublisher eventPublisher,
                   ILogger<Program> logger,
                   CancellationToken cancellationToken) =>
            {
                var result = await useCase.Execute(shortenedCode, cancellationToken);

                if (!result.IsSuccess)
                    return Results.BadRequest(ResultJson.Failure(result.Errors));

                if (result.Data.Status == RetrieveOriginalUrlStatus.Found)
                {
                    var query = httpContext.Request.Query;
                    var ev = new UrlClickedEvent
                    {
                        ShortCode = shortenedCode,
                        Timestamp = DateTime.UtcNow,
                        IpAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                                    ?? httpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                        Referer = httpContext.Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : null,
                        UtmSource = query["utm_source"].ToString() is { Length: > 0 } s ? s : null,
                        UtmMedium = query["utm_medium"].ToString() is { Length: > 0 } m ? m : null,
                        UtmCampaign = query["utm_campaign"].ToString() is { Length: > 0 } c ? c : null
                    };

                    try
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
                        await eventPublisher.PublishAsync(ev, cts.Token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to publish UrlClickedEvent for {ShortCode}", shortenedCode);
                    }
                }

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

                if (!result.IsSuccess)
                    return Results.BadRequest(ResultJson.Failure(result.Errors));

                return Results.Created($"/{result.Data.ShortenCode}", result);
            });

        return app;
    }
}
