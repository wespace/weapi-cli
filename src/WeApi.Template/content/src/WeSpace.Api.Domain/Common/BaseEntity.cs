namespace WeSpace.Api.Domain.Common;

public abstract class BaseEntity
{
#if useIntId
    public int Id { get; set; }
#elif useLongId
    public long Id { get; set; }
#else
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
#endif
}
