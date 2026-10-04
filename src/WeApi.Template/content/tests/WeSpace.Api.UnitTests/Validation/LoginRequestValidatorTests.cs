using WeSpace.Api.Application.DTOs.Auth;
using WeSpace.Api.Application.Validators;
using Xunit;

namespace WeSpace.Api.UnitTests.Validation;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new LoginRequest("user@example.com", "SecretPass123!");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("not-an-email", "password")]
    [InlineData("user@example.com", "")]
    public async Task Validate_WithInvalidRequest_ShouldFail(string email, string password)
    {
        // Arrange
        var request = new LoginRequest(email, password);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }
}
