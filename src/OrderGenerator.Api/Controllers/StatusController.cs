using Microsoft.AspNetCore.Mvc;
using OrderGenerator.Api.Dtos;
using OrderGenerator.Api.Fix;

namespace OrderGenerator.Api.Controllers;

[ApiController]
[Route("api/status")]
public class StatusController(IOrderGateway orderGateway) : ControllerBase
{
    [HttpGet]
    public ActionResult<StatusResponse> Get()
    {
        return Ok(new StatusResponse(orderGateway.IsConnected));
    }
}
