using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommonTestUtilities.Builders.Requests.Url;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Minify.Infrastructure.DataAccess;
using WebApi.Test.Infrastructure;

namespace WebApi.Test.Endpoints.Url;

public class ShortenUrlWebApiTests(MinifyApiFixture fixture) : IClassFixture<MinifyApiFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Should_Return201_When_RequestIsValid()
    {
        var request = RequestShortenUrlJsonBuilder.Build();

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/shorten-url", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ShortenUrlResponse>(JsonOptions);
        
        body.Should().NotBeNull();
        body.IsSuccess.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data.ShortenCode.Should().NotBeNullOrEmpty();
        body.Data.ExpiresAt.Should().BeCloseTo(request.ExpiresAt, TimeSpan.FromSeconds(1));

        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MinifyDbContext>();
        
        var shortUrlEntity = await dbContext.ShortUrls.AsNoTracking().FirstAsync(u => u.ShortenCode == body.Data.ShortenCode);
        
        shortUrlEntity.Should().NotBeNull();
        shortUrlEntity.LongUrl.Should().Be(request.Url);
        shortUrlEntity.ExpiresAt.Should().BeCloseTo(request.ExpiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Should_Return400_When_UrlIsInvalid()
    {
        var request = RequestShortenUrlJsonBuilder.BuildWithInvalidUrl();

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/shorten-url", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var body = await response.Content.ReadFromJsonAsync<ShortenUrlResponse>(JsonOptions);
        
        body.Should().NotBeNull();
        body.IsSuccess.Should().BeFalse();
        body.Errors.Should().Contain("Property 'url' must be a valid absolute http or https URL.");
    }

    [Fact]
    public async Task Should_Return400_When_ExpiresAtIsInThePast()
    {
        var request = RequestShortenUrlJsonBuilder.BuildWithPastExpiration();

        var response = await fixture.HttpClient.PostAsJsonAsync("/api/shorten-url", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var body = await response.Content.ReadFromJsonAsync<ShortenUrlResponse>(JsonOptions);
        
        body.Should().NotBeNull();
        body.IsSuccess.Should().BeFalse();
        body.Errors.Should().Contain("Property 'expiresAt' must be a date in the future.");
    }

    private record ShortenUrlResponse(bool IsSuccess, List<string> Errors, ShortenUrlData? Data);

    private record ShortenUrlData(string ShortenCode, DateTime ExpiresAt);
}
