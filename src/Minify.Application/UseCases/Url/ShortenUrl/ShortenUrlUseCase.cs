using Minify.Communication.Requests.Url;
using Minify.Communication.Responses;
using Minify.Communication.Responses.Url;
using Minify.Domain.Entities;
using Minify.Domain.Repositories;
using Minify.Domain.Repositories.ShortUrl;

namespace Minify.Application.UseCases.Url.ShortenUrl;

public class ShortenUrlUseCase(IShortUrlWriteOnlyRepository writeOnlyRepository, IUnitOfWork unitOfWork)
    : IShortenUrlUseCase
{
    public async Task<ResultJson<ResponseShortenUrlJson>> Execute(RequestShortenUrlJson request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = Validate<ResponseShortenUrlJson>(request);

        if (!validationResult.IsSuccess)
            return validationResult;

        var payload = new ShortUrlEntity
        {
            ShortenCode = "abc123",
            ExpiresAt = request.ExpiresAt,
            LongUrl = request.Url
        };

        await writeOnlyRepository.Add(payload, cancellationToken);
        await unitOfWork.Commit(cancellationToken);

        return ResultJson<ResponseShortenUrlJson>.Success(new ResponseShortenUrlJson
        {
            ExpiresAt = payload.ExpiresAt,
            ShortenCode = payload.ShortenCode
        });
    }

    private static ResultJson<T> Validate<T>(RequestShortenUrlJson request)
    {
        var validationResult = new ShortenUrlValidator().Validate(request);

        if (validationResult.IsValid)
            return ResultJson<T>.Success(default!);

        var errors = validationResult.Errors
            .Select(error => error.ErrorMessage)
            .ToList();

        return ResultJson<T>.Failure(errors);
    }
}
