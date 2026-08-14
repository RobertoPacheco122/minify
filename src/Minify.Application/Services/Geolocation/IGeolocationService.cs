namespace Minify.Application.Services.Geolocation;

public interface IGeolocationService
{
    Task<GeolocationResult?> GetAsync(string ipAddress, CancellationToken cancellationToken = default);
}

public record GeolocationResult(string Country, string City);
