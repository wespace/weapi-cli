namespace WeSpace.Api.Application.DTOs.Auth;

public record AuthResponse(
    string AccessToken,
    DateTime ExpiresAt,
    UserResponse User);
