using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using WeSpace.Api.Domain.Entities;
using WeSpace.Api.Domain.Enums;
using WeSpace.Api.Infrastructure.Authentication;
using Xunit;

namespace WeSpace.Api.UnitTests.Authentication;

public class JwtTokenGeneratorTests
{
    private readonly JwtTokenGenerator _generator;
    private readonly JwtOptions _options;

    public JwtTokenGeneratorTests()
    {
        _options = new JwtOptions
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SecretKey = "SuperSecretKeyThatIsAtLeast32BytesLong123456!",
            ExpirationMinutes = 60
        };

        _generator = new JwtTokenGenerator(Options.Create(_options));
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidJwtTokenAndFutureExpiration()
    {
        // Arrange
        var user = User.Create("Jane", "Doe", "jane@example.com", "dummy_hash", UserRole.User);

        // Act
        var (token, expiresAt) = _generator.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.True(expiresAt > DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));

        var jwt = handler.ReadJwtToken(token);
        Assert.Equal(_options.Issuer, jwt.Issuer);
        Assert.Contains(_options.Audience, jwt.Audiences);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    }
}
