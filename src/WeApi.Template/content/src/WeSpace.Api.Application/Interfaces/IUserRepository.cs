using WeSpace.Api.Domain.Entities;

namespace WeSpace.Api.Application.Interfaces;

public interface IUserRepository
{
#if useIntId
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
#elif useLongId
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
#else
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
#endif
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
