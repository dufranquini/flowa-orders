using Microsoft.Extensions.Logging.Abstractions;
using OrderGenerator.Api.Fix;

namespace OrderGenerator.Api.Tests.Fix;

public class FixInitiatorServiceTests
{
    [Fact]
    public async Task StopAsync_WithoutStart_DoesNotThrow()
    {
        // Arrange
        var gateway = new FixOrderGateway(new QuickFixSession(), NullLogger<FixOrderGateway>.Instance);
        var service = new FixInitiatorService(gateway);

        // Act
        var exception = await Record.ExceptionAsync(() => service.StopAsync(CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }
}
