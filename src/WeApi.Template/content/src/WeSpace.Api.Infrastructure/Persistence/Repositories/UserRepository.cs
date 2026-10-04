using Microsoft.EntityFrameworkCore;
using WeSpace.Api.Application.Interfaces;
using WeSpace.Api.Domain.Entities;

namespace WeSpace.Api.Infrastructure.Persistence.Repositories;

public class UserRepository(ApplicationDbContext dbContext) : IUserRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

#if useIntId
    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
#elif useLongId
    public async Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
#else
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
#endif
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
