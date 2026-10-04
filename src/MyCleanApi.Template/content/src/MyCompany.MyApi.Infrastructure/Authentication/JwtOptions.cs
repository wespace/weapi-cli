using System.ComponentModel.DataAnnotations;

namespace MyCompany.MyApi.Infrastructure.Authentication;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [MinLength(32, ErrorMessage = "SecretKey must be at least 32 characters (256 bits).")]
    public string SecretKey { get; set; } = string.Empty;

    [Range(1, 43200, ErrorMessage = "ExpirationMinutes must be between 1 and 43200.")]
    public int ExpirationMinutes { get; set; } = 60;
}
