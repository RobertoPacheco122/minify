namespace Minify.Application.Services.ShortCode;

public interface IShortCodeGenerator
{
    Task<string> Generate(CancellationToken cancellationToken = default);
}
