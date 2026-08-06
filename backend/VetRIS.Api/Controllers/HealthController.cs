using Microsoft.AspNetCore.Mvc;
using VetRIS.Api.Dtos;

namespace VetRIS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse(
            Status: "ok",
            Service: "vetris-api",
            Version: "0.1.0-scaffold",
            TimestampUtc: DateTime.UtcNow
        ));
    }
}
