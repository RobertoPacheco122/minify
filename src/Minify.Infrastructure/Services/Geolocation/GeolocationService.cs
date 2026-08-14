using System.Net.Http.Json;
using Minify.Application.Services.Geolocation;

namespace Minify.Infrastructure.Services.Geolocation;

public class GeolocationService(HttpClient httpClient) : IGeolocationService
{
    public async Task<GeolocationResult?> GetAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<IpApiResponse>(
                $"json/{ipAddress}?fields=status,country,city",
                cancellationToken);

            if (response is null || response.Status != "success")
                return null;

            return new GeolocationResult(response.Country, response.City);
        }
        catch
        {
            return null;
        }
    }

    private record IpApiResponse(string Status, string Country, string City);
}
