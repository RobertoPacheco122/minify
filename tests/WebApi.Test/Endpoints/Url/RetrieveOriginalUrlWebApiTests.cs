using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommonTestUtilities.Builders.Entities;
using CommonTestUtilities.Builders.Requests.Url;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Minify.Infrastructure.DataAccess;
using WebApi.Test.Infrastructure;

namespace WebApi.Test.Endpoints.Url;

public class RetrieveOriginalUrlWebApiTests(MinifyApiFixture fixture) : IClassFixture<MinifyApiFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    
    [Fact]
    public async Task Should_Return301_When_UrlExists()
    {
        var createRequest = RequestShortenUrlJsonBuilder.Build();
        var creationResponse = await fixture.HttpClient.PostAsJsonAsync("/api/shorten-url", createRequest);
        
        var body = await creationResponse.Content.ReadFromJsonAsync<ShortenUrlResponse>(JsonOptions);
        var shortenCode = body!.Data!.ShortenCode;

        var response = await fixture.HttpClient.GetAsync($"/{shortenCode}");

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.OriginalString.Should().Be(createRequest.Url);
    }
    
    [Fact]
    public async Task Should_Return404_When_CodeDoesNotExist()
    {
        var response = await fixture.HttpClient.GetAsync("/zzzz");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task Should_Return410_When_UrlIsExpired()
    {
        var expiredEntity = ShortUrlEntityBuilder.BuildExpired();
        expiredEntity.ShortenCode = "wxyz";

        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MinifyDbContext>();
        dbContext.ShortUrls.Add(expiredEntity);
        await dbContext.SaveChangesAsync();

        var response = await fixture.HttpClient.GetAsync($"/{expiredEntity.ShortenCode}");

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
    }
    
    private record ShortenUrlResponse(bool IsSuccess, List<string> Errors, ShortenUrlData? Data);
    private record ShortenUrlData(string ShortenCode, DateTime ExpiresAt);
}
