using MediatR;
using Microsoft.AspNetCore.Mvc;
using VetRIS.Application.Common.Interfaces;

namespace VetRIS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    private ICurrentUserService? _currentUser;
    protected ICurrentUserService CurrentUser => _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
}
