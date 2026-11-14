using Domain;

namespace OrderGenerator.Api.Dtos;

public record OrderResponse(
  string ClOrdId,
  string OrderId,
  string ExecId,
  OrderStatus Status,
  string Symbol,
  OrderSide Side,
  int Quantity,
  decimal Price,
  string? Text,
  int? RejectReason,
  DateTimeOffset ReceivedAt);