using MyCompany.MyApi.Domain.Entities;

namespace MyCompany.MyApi.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
