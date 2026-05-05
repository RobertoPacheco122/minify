using Minify.Application.Services.ShortCode;
using Moq;

namespace CommonTestUtilities.Mocks.Services.ShortCode;

public class ShortCodeGeneratorMock
{
    public Mock<IShortCodeGenerator> Mock { get; } = new();

    public ShortCodeGeneratorMock(string code = "abcd")
    {
        Mock.Setup(g => g.Generate(It.IsAny<CancellationToken>()))
            .ReturnsAsync(code);
    }
}
