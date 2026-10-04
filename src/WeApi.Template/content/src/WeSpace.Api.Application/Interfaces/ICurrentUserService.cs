namespace WeSpace.Api.Application.Interfaces;

public interface ICurrentUserService
{
#if useIntId
    int? UserId { get; }
#elif useLongId
    long? UserId { get; }
#else
    Guid? UserId { get; }
#endif
    string? Email { get; }
    bool IsAuthenticated { get; }
}
