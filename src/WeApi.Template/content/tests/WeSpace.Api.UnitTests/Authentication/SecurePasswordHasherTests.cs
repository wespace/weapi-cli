using WeSpace.Api.Infrastructure.Authentication;
using Xunit;

namespace WeSpace.Api.UnitTests.Authentication;

public class SecurePasswordHasherTests
{
    private readonly SecurePasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldReturnValidSaltedHash()
    {
        // Arrange
        const string password = "TestPassword@123";

        // Act
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        // Assert
        Assert.NotNull(hash1);
        Assert.NotNull(hash2);
        // Salt ensures two identical passwords generate different hashes
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        const string password = "TestPassword@123";
        var hash = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        const string password = "TestPassword@123";
        const string wrongPassword = "WrongPassword@999";
        var hash = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid_format")]
    public void VerifyPassword_WithInvalidHash_ShouldReturnFalse(string invalidHash)
    {
        // Act
        var result = _hasher.VerifyPassword("SomePassword", invalidHash);

        // Assert
        Assert.False(result);
    }
}
