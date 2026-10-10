namespace WeSpace.Api.Domain.Common;

public abstract class BaseEntity
{
#if useIntId
    public int Id { get; set; }
#elif useLongId
    public long Id { get; set; }
#else
#if NET9_0_OR_GREATER
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
#else
    public Guid Id { get; protected set; } = Guid.NewGuid();
#endif
#endif
}
