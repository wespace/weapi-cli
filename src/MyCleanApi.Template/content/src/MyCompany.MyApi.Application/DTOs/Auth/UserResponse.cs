namespace MyCompany.MyApi.Application.DTOs.Auth;

public record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    DateTimeOffset CreatedAt);
