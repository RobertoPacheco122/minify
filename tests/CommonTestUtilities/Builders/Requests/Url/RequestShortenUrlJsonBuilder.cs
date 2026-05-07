using Minify.Communication.Requests.Url;

namespace CommonTestUtilities.Builders.Requests.Url;

public class RequestShortenUrlJsonBuilder
{
    public static RequestShortenUrlJson Build() =>
        new()
        {
            Url = "https://example.com",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

    public static RequestShortenUrlJson BuildWithInvalidUrl() =>
        new()
        {
            Url = "not-a-url",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

    public static RequestShortenUrlJson BuildWithPastExpiration() =>
        new()
        {
            Url = "https://example.com",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
}
