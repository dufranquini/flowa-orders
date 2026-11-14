using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Api.Dtos;
using OrderGenerator.Api.Fix;
using OrderGenerator.Api.Repositories;

namespace OrderGenerator.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderHistoryRepository orderHistoryRepository, IOrderGateway orderGateway) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetAll(CancellationToken cancellation)
    {
        var orders = await orderHistoryRepository.GetAllAsync();
        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult> Post(NewOrderRequest request, CancellationToken cancellation)
    {
        try
        {
            OrderResponse response = await orderGateway.SendAsync(request, cancellation);
            await orderHistoryRepository.AddAsync(response, cancellation);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError, title: "Error sending order");
        }
    }
}
