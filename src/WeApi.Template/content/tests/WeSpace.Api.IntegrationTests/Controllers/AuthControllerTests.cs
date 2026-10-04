using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WeSpace.Api.Application.DTOs.Auth;
using WeSpace.Api.IntegrationTests.Common;
using Xunit;

namespace WeSpace.Api.IntegrationTests.Controllers;

public class AuthControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidData_Returns201CreatedAndToken()
    {
        // Arrange
        var email = $"register_{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest("Test", "User", email, "Password@123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.Equal(email, result.User.Email);
        Assert.Equal("Test", result.User.FirstName);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409Conflict()
    {
        // Arrange
        var email = $"duplicate_{Guid.NewGuid()}@example.com";
        var request = new RegisterRequest("Test", "User", email, "Password@123");

        // First registration
        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Act - Second registration with same email
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidData_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest("", "", "invalid-email", "short");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200OKAndToken()
    {
        // Arrange - Register user first
        var email = $"login_{Guid.NewGuid()}@example.com";
        const string password = "Password@123";
        var registerRequest = new RegisterRequest("Login", "Tester", email, password);
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(email, password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.Equal(email, result.User.Email);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401Unauthorized()
    {
        // Arrange
        var email = $"wrongpass_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest("Login", "Tester", email, "Password@123");
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(email, "WrongPassword@999");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidJwtToken_Returns200OKAndUserProfile()
    {
        // Arrange - Register to get valid token
        var email = $"me_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest("Me", "User", email, "Password@123");
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authResult = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResult);

        // Attach JWT
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(profile);
        Assert.Equal(email, profile.Email);
        Assert.Equal("Me", profile.FirstName);
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401Unauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
