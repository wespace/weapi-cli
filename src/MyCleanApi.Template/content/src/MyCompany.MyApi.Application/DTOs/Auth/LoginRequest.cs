namespace MyCompany.MyApi.Application.DTOs.Auth;

public record LoginRequest(
    string Email,
    string Password);
