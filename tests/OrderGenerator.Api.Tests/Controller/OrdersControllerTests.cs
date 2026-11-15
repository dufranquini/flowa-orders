using Domain;
using OrderGenerator.Api.Dtos;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderGenerator.Api.Tests.Controller;

public class OrdersControllerTests : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public OrdersControllerTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static object ValidOrder(string side = "Buy") =>
      new { symbol = "PETR4", side, quantity = 100, price = 35.42m };

    [Fact]
    public async Task Post_ValidOrder_Returns200WithTheExecutionReport()
    {
        // Arrange
        var order = ValidOrder();

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", order);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>(Json);
        Assert.NotNull(body);
        Assert.Equal(OrderStatus.New, body.Status);
        Assert.Equal("PETR4", body.Symbol);
        Assert.Equal(100, body.Quantity);
        Assert.Equal(35.42m, body.Price);
        var sent = Assert.Single(_factory.Gateway.Received);
        Assert.Equal(OrderSide.Buy, sent.Side);
    }

    [Fact]
    public async Task Post_ValidOrder_SerializesEnumsAsText()
    {
        // Arrange
        var order = ValidOrder("Sell");

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", order);

        // Assert
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"New\"", json);
        Assert.Contains("\"side\":\"Sell\"", json);
    }

    [Fact]
    public async Task Post_OrderRejectedByTheAccumulator_Returns200WithStatusRejected()
    {
        // Arrange
        _factory.Gateway.Respond = request => FakeOrderGateway.Rejected(request, "Order exceeds limit.");

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", ValidOrder());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>(Json);
        Assert.Equal(OrderStatus.Rejected, body!.Status);
        Assert.Equal("Order exceeds limit.", body.Text);
        Assert.Equal(3, body.RejectReason);
    }

    [Fact]
    public async Task Post_UnknownSide_Returns400AndDoesNotSend()
    {
        // Arrange
        var order = ValidOrder("Short");

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", order);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_factory.Gateway.Received);
    }

    [Fact]
    public async Task Post_FractionalQuantity_Returns400AndDoesNotSend()
    {
        // Arrange
        var order = new { symbol = "PETR4", side = "Buy", quantity = 1.5m, price = 10m };

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", order);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_factory.Gateway.Received);
    }

    [Fact]
    public async Task Post_WhenGatewayFails_Returns500WithTheErrorMessage()
    {
        // Arrange
        _factory.Gateway.Respond = _ => throw new Exception("Not connected to FIX session");

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", ValidOrder());

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Not connected to FIX session", json);
    }

    [Fact]
    public async Task GetAll_AfterTwoOrders_ReturnsNewestFirst()
    {
        // Arrange
        await _client.PostAsJsonAsync("/api/orders", ValidOrder("Buy"));
        await _client.PostAsJsonAsync("/api/orders", ValidOrder("Sell"));

        // Act
        var history = await _client.GetFromJsonAsync<List<OrderResponse>>("/api/orders", Json);

        // Assert
        Assert.Equal(2, history!.Count);
        Assert.Equal(OrderSide.Sell, history[0].Side);
        Assert.Equal(OrderSide.Buy, history[1].Side);
    }

    [Fact]
    public async Task GetAll_OrderThatFailedToSend_IsNotInTheHistory()
    {
        // Arrange
        _factory.Gateway.Respond = _ => throw new Exception("Not connected to FIX session");
        await _client.PostAsJsonAsync("/api/orders", ValidOrder());

        // Act
        var history = await _client.GetFromJsonAsync<List<OrderResponse>>("/api/orders", Json);

        // Assert
        Assert.Empty(history!);
    }
}