namespace WeSpace.Api.Application.DTOs.Auth;

public record UserResponse(
#if useIntId
    int Id,
#elif useLongId
    long Id,
#else
    Guid Id,
#endif
    string FirstName,
    string LastName,
    string Email,
    string Role,
    DateTimeOffset CreatedAt);
