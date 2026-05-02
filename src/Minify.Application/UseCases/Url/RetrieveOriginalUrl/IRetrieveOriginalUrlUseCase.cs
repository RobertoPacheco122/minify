using Minify.Communication.Responses;
using Minify.Communication.Responses.Url;

namespace Minify.Application.UseCases.Url.RetrieveOriginalUrl;

public interface IRetrieveOriginalUrlUseCase
{
    Task<ResultJson<ResponseRetrieveOriginalUrlJson>> Execute(string shortenedCode,
        CancellationToken cancellationToken = default);
}
