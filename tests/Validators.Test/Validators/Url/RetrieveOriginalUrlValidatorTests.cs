using FluentAssertions;
using Minify.Application.UseCases.Url.RetrieveOriginalUrl;

namespace Validators.Test.Validators.Url;

public class RetrieveOriginalUrlValidatorTests
{
    private readonly RetrieveOriginalUrlValidator _validator = new();

    [Fact]
    public void Should_Pass_When_CodeIsValid()
    {
        var result = _validator.Validate("abcd");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_CodeIsEmpty()
    {
        var result = _validator.Validate(string.Empty);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData("aa")]
    [InlineData("aaa")]
    public void Should_Fail_When_CodeIsTooShort(string code)
    {
        var result = _validator.Validate(code);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CodeIsTooLong()
    {
        var result = _validator.Validate("abcdefgh");

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("abcd/")]
    [InlineData("abcd!")]
    [InlineData("abcd.")]
    [InlineData("abcd?")]
    [InlineData("abcd&")]
    public void Should_Fail_When_CodeHasNonBase62Characters(string code)
    {
        var result = _validator.Validate(code);

        result.IsValid.Should().BeFalse();
    }
}
