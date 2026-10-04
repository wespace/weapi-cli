using System.Security.Cryptography;
using WeSpace.Api.Application.Interfaces;

namespace WeSpace.Api.Infrastructure.Authentication;

public class SecurePasswordHasher : IPasswordHasher
{
    private const int SaltSize = 32; // 256 bits
    private const int KeySize = 64;  // 512 bits
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;
    private const char Delimiter = ';';

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var subKey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            KeySize);

        return string.Join(
            Delimiter,
            Convert.ToBase64String(salt),
            Iterations,
            Algorithm.Name,
            Convert.ToBase64String(subKey));
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split(Delimiter);
        if (parts.Length != 4)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var iterations = int.Parse(parts[1]);
            var algorithm = new HashAlgorithmName(parts[2]);
            var expectedKey = Convert.FromBase64String(parts[3]);

            var actualKey = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                algorithm,
                expectedKey.Length);

            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
        catch
        {
            return false;
        }
    }
}
