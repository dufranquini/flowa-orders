using Domain;

namespace OrderGenerator.Api.Dtos;

public record NewOrderRequest(
    string? Symbol, 
    OrderSide? Side, 
    int Quantity, 
    decimal Price);