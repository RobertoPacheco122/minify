using Minify.Communication.Requests.Url;
using Minify.Communication.Responses;
using Minify.Communication.Responses.Url;

namespace Minify.Application.UseCases.Url.ShortenUrl;

public interface IShortenUrlUseCase
{
    Task<ResultJson<ResponseShortenUrlJson>> Execute(RequestShortenUrlJson request, CancellationToken cancellationToken = default);
}
