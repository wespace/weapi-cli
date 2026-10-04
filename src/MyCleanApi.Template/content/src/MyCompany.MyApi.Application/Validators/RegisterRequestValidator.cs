using System.Text.RegularExpressions;
using FluentValidation;
using MyCompany.MyApi.Application.DTOs.Auth;

namespace MyCompany.MyApi.Application.Validators;

public partial class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must contain at least 8 characters.")
            .Must(HasUpperCase).WithMessage("Password must contain at least one uppercase letter.")
            .Must(HasLowerCase).WithMessage("Password must contain at least one lowercase letter.")
            .Must(HasDigit).WithMessage("Password must contain at least one digit.")
            .Must(HasSpecialChar).WithMessage("Password must contain at least one special character.");
    }

    private static bool HasUpperCase(string password) =>
        !string.IsNullOrEmpty(password) && password.Any(char.IsUpper);

    private static bool HasLowerCase(string password) =>
        !string.IsNullOrEmpty(password) && password.Any(char.IsLower);

    private static bool HasDigit(string password) =>
        !string.IsNullOrEmpty(password) && password.Any(char.IsDigit);

    private static bool HasSpecialChar(string password) =>
        !string.IsNullOrEmpty(password) && password.Any(ch => !char.IsLetterOrDigit(ch));
}
