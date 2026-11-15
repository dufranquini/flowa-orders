using OrderGenerator.Api.Dtos;
using System;
using System.Net.Http.Json;

namespace OrderGenerator.Api.Tests.Controller;

public class StatusControllerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_ReturnsTheFixConnectionState(bool connected)
    {
        // Arrange
        using var factory = new ApiFactory();
        factory.Gateway.IsConnected = connected;
        using var client = factory.CreateClient();

        // Act
        var status = await client.GetFromJsonAsync<StatusResponse>("/api/status");

        // Assert
        Assert.Equal(connected, status!.FixConnected);
    }
}