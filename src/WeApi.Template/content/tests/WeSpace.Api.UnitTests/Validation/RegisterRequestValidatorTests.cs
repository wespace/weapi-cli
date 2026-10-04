using WeSpace.Api.Application.DTOs.Auth;
using WeSpace.Api.Application.Validators;
using Xunit;

namespace WeSpace.Api.UnitTests.Validation;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidRequest_ShouldPass()
    {
        // Arrange
        var request = new RegisterRequest("John", "Doe", "john.doe@example.com", "SecurePass@123");

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "Doe", "john@example.com", "Password@123")]
    [InlineData("John", "", "john@example.com", "Password@123")]
    [InlineData("John", "Doe", "", "Password@123")]
    [InlineData("John", "Doe", "not-an-email", "Password@123")]
    [InlineData("John", "Doe", "john@example.com", "")]
    [InlineData("John", "Doe", "john@example.com", "short1!")] // < 8 chars
    [InlineData("John", "Doe", "john@example.com", "password@123")] // missing uppercase
    [InlineData("John", "Doe", "john@example.com", "PASSWORD@123")] // missing lowercase
    [InlineData("John", "Doe", "john@example.com", "PasswordAAAA!")] // missing digit
    [InlineData("John", "Doe", "john@example.com", "Password12345")] // missing special char
    public async Task Validate_WithInvalidRequest_ShouldFail(
        string firstName, string lastName, string email, string password)
    {
        // Arrange
        var request = new RegisterRequest(firstName, lastName, email, password);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }
}
