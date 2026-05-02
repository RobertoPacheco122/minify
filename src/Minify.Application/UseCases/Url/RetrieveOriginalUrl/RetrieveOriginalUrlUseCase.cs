using Minify.Application.Services.Caching;
using Minify.Communication.Responses;
using Minify.Communication.Responses.Url;
using Minify.Domain.Repositories;
using Minify.Domain.Repositories.ShortUrl;

namespace Minify.Application.UseCases.Url.RetrieveOriginalUrl;

public class RetrieveOriginalUrlUseCase(
    IShortUrlReadOnlyRepository readOnlyRepository,
    IUrlCacheService urlCacheService) : IRetrieveOriginalUrlUseCase
{
    public async Task<ResultJson<ResponseRetrieveOriginalUrlJson>> Execute(string shortenedCode,
        CancellationToken cancellationToken = default)
    {
        var validationResult = Validate<ResponseRetrieveOriginalUrlJson>(shortenedCode);

        if (!validationResult.IsSuccess)
            return validationResult;

        var cachedUrl = await urlCacheService.Get(shortenedCode, cancellationToken);

        if (cachedUrl is not null)
            return ResultJson<ResponseRetrieveOriginalUrlJson>.Success(new ResponseRetrieveOriginalUrlJson
            {
                Status = RetrieveOriginalUrlStatus.Found,
                LongUrl = cachedUrl.LongUrl
            });

        var urlOnDatabase = await readOnlyRepository.GetByShortCode(shortenedCode, cancellationToken);

        if (urlOnDatabase is null)
            return ResultJson<ResponseRetrieveOriginalUrlJson>.Success(new ResponseRetrieveOriginalUrlJson
            {
                Status = RetrieveOriginalUrlStatus.NotFound
            });

        if (urlOnDatabase.ExpiresAt < DateTime.UtcNow)
            return ResultJson<ResponseRetrieveOriginalUrlJson>.Success(new ResponseRetrieveOriginalUrlJson
            {
                Status = RetrieveOriginalUrlStatus.Expired
            });

        await urlCacheService.Set(shortenedCode,
            new CachedUrl { LongUrl = urlOnDatabase.LongUrl, ExpiresAt = urlOnDatabase.ExpiresAt }, cancellationToken);

        return ResultJson<ResponseRetrieveOriginalUrlJson>.Success(new ResponseRetrieveOriginalUrlJson
        {
            Status = RetrieveOriginalUrlStatus.Found,
            LongUrl = urlOnDatabase.LongUrl
        });
    }

    private static ResultJson<T> Validate<T>(string shortenedCode)
    {
        var validationResult = new RetrieveOriginalUrlValidator().Validate(shortenedCode);

        if (validationResult.IsValid)
            return ResultJson<T>.Success(default!);

        var errors = validationResult.Errors
            .Select(error => error.ErrorMessage)
            .ToList();

        return ResultJson<T>.Failure(errors);
    }
}
