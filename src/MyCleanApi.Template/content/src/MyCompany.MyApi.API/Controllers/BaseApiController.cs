using Microsoft.AspNetCore.Mvc;

namespace MyCompany.MyApi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
}
