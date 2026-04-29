using Minify.Communication.Requests.Url;
using Minify.Communication.Responses.Url;

namespace Minify.Application.UseCases.Url.ShortenUrl;

public interface IShortenUrlUseCase
{
    Task<ResponseShortenUrlJson> Execute(RequestShortenUrlJson request, CancellationToken cancellationToken = default);
}
