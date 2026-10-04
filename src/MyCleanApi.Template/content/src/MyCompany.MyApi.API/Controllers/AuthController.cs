using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCompany.MyApi.Application.DTOs.Auth;
using MyCompany.MyApi.Application.Interfaces;

namespace MyCompany.MyApi.API.Controllers;

public class AuthController(
    IAuthenticationService authService,
    ICurrentUserService currentUserService) : BaseApiController
{
    private readonly IAuthenticationService _authService = authService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">User registration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response containing access token and user profile.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetMe), new { }, response);
    }

    /// <summary>
    /// Authenticates a user and generates a JWT access token.
    /// </summary>
    /// <param name="request">User login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response containing access token and user profile.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Gets the profile of the currently authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User profile.</returns>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
                     ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var response = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(response);
    }
}
