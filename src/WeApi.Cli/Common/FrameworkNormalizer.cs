using System.Collections.Frozen;

namespace WeApi.Cli.Common;

public static class FrameworkNormalizer
{
    private static readonly FrozenSet<string> SupportedFrameworks = new[]
    {
        "net6.0",
        "net7.0",
        "net8.0",
        "net9.0",
        "net10.0"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();

        var candidate = trimmed switch
        {
            "6" or "6.0" or "net6" or "net6.0" or "NET6" or "NET6.0" => "net6.0",
            "7" or "7.0" or "net7" or "net7.0" or "NET7" or "NET7.0" => "net7.0",
            "8" or "8.0" or "net8" or "net8.0" or "NET8" or "NET8.0" => "net8.0",
            "9" or "9.0" or "net9" or "net9.0" or "NET9" or "NET9.0" => "net9.0",
            "10" or "10.0" or "net10" or "net10.0" or "NET10" or "NET10.0" => "net10.0",
            _ => trimmed
        };

        if (SupportedFrameworks.TryGetValue(candidate, out var actual))
        {
            normalized = actual;
            return true;
        }

        return false;
    }

    public static string Normalize(string input)
    {
        if (TryNormalize(input, out var normalized))
        {
            return normalized;
        }

        throw new ArgumentException(
            $"Unsupported target framework '{input}'. Supported frameworks are: {string.Join(", ", SupportedFrameworks)}.");
    }

    public static bool IsSupported(string framework) =>
        SupportedFrameworks.Contains(framework);

    public static IReadOnlyCollection<string> GetSupportedFrameworks() =>
        SupportedFrameworks;
}
