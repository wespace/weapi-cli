using WeSpace.Api.Application.Common.Exceptions;
using WeSpace.Api.Application.DTOs.Auth;
using WeSpace.Api.Application.Interfaces;
using WeSpace.Api.Application.Services;
using WeSpace.Api.Application.Validators;
using WeSpace.Api.Domain.Entities;
using WeSpace.Api.Domain.Enums;
using Xunit;

namespace WeSpace.Api.UnitTests.Authentication;

public class AuthenticationServiceTests
{
    private readonly InMemoryUserRepository _userRepository = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly FakeJwtTokenGenerator _tokenGenerator = new();
    private readonly AuthenticationService _service;

    public AuthenticationServiceTests()
    {
        _service = new AuthenticationService(
            _userRepository,
            _passwordHasher,
            _tokenGenerator,
            new RegisterRequestValidator(),
            new LoginRequestValidator());
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ShouldCreateUserAndReturnToken()
    {
        // Arrange
        var request = new RegisterRequest("Alice", "Smith", "alice@example.com", "Password@123");

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("fake-token", response.AccessToken);
        Assert.Equal("Alice", response.User.FirstName);
        Assert.Equal("Smith", response.User.LastName);
        Assert.Equal("alice@example.com", response.User.Email);

        var savedUser = await _userRepository.GetByEmailAsync("alice@example.com");
        Assert.NotNull(savedUser);
        Assert.Equal("hashed_Password@123", savedUser.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowConflictException()
    {
        // Arrange
        var existingUser = User.Create("Alice", "Smith", "alice@example.com", "hash");
        await _userRepository.AddAsync(existingUser);

        var request = new RegisterRequest("Alice", "Smith", "alice@example.com", "Password@123");

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var request = new RegisterRequest("", "Smith", "invalid-email", "short");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        const string rawPassword = "Password@123";
        var user = User.Create("Bob", "Jones", "bob@example.com", _passwordHasher.HashPassword(rawPassword));
        await _userRepository.AddAsync(user);

        var request = new LoginRequest("bob@example.com", rawPassword);

        // Act
        var response = await _service.LoginAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("fake-token", response.AccessToken);
        Assert.Equal("bob@example.com", response.User.Email);
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var user = User.Create("Bob", "Jones", "bob@example.com", _passwordHasher.HashPassword("Password@123"));
        await _userRepository.AddAsync(user);

        var request = new LoginRequest("bob@example.com", "WrongPassword@999");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@example.com", "Password@123");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithDeactivatedUser_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var user = User.Create("Inactive", "User", "inactive@example.com", _passwordHasher.HashPassword("Password@123"));
        user.Deactivate();
        await _userRepository.AddAsync(user);

        var request = new LoginRequest("inactive@example.com", "Password@123");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _service.LoginAsync(request));
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenUserExists_ShouldReturnUserProfile()
    {
        // Arrange
        var user = User.Create("Claire", "Redfield", "claire@example.com", "hash");
        await _userRepository.AddAsync(user);

        // Act
        var response = await _service.GetCurrentUserAsync(user.Id);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(user.Id, response.Id);
        Assert.Equal("Claire", response.FirstName);
        Assert.Equal("claire@example.com", response.Email);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenUserNotFound_ShouldThrowNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetCurrentUserAsync(Guid.NewGuid()));
    }

    // In-memory test test doubles
    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed_{password}";

        public bool VerifyPassword(string password, string passwordHash) =>
            passwordHash == $"hashed_{password}";
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public (string Token, DateTime ExpiresAt) GenerateToken(User user) =>
            ("fake-token", DateTime.UtcNow.AddHours(1));
    }
}
