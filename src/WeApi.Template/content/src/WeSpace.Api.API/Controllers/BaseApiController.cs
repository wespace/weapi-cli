using Microsoft.AspNetCore.Mvc;

namespace WeSpace.Api.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
}
