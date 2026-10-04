using WeSpace.Api.Application.DTOs.Auth;

namespace WeSpace.Api.Application.Interfaces;

public interface IAuthenticationService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
#if useIntId
    Task<UserResponse> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
#elif useLongId
    Task<UserResponse> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default);
#else
    Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
#endif
}
