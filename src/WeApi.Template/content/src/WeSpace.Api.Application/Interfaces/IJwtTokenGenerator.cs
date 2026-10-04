using WeSpace.Api.Domain.Entities;

namespace WeSpace.Api.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
