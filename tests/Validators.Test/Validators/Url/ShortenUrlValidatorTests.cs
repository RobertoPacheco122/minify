using CommonTestUtilities.Builders.Requests.Url;
using FluentAssertions;
using Minify.Application.UseCases.Url.ShortenUrl;

namespace Validators.Test.Validators.Url;

public class ShortenUrlValidatorTests
{
    private readonly ShortenUrlValidator _validator = new();

    [Fact]
    public void Should_Pass_When_RequestIsValid()
    {
        var request = RequestShortenUrlJsonBuilder.Build();

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_UrlIsEmpty()
    {
        var request = RequestShortenUrlJsonBuilder.Build();
        request.Url = string.Empty;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Property 'url' is required.");
    }

    [Fact]
    public void Should_Fail_When_UrlIsRelative()
    {
        var request = RequestShortenUrlJsonBuilder.Build();
        request.Url = "/path/only";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Property 'url' must be a valid absolute http or https URL.");
    }

    [Fact]
    public void Should_Fail_When_UrlSchemeIsNotHttpOrHttps()
    {
        var request = RequestShortenUrlJsonBuilder.Build();
        request.Url = "ftp://example.com";

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Property 'url' must be a valid absolute http or https URL.");
    }

    [Fact]
    public void Should_Fail_When_ExpiresAtIsInThePast()
    {
        var request = RequestShortenUrlJsonBuilder.BuildWithPastExpiration();

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Property 'expiresAt' must be a date in the future.");
    }

    [Fact]
    public void Should_Fail_When_ExpiresAtIsNow()
    {
        var request = RequestShortenUrlJsonBuilder.Build();
        request.ExpiresAt = DateTime.UtcNow;

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Property 'expiresAt' must be a date in the future.");
    }
}
