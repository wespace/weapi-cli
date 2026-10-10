using System.Collections.Frozen;

namespace WeApi.Cli.Services;

public static class PackageCompatibility
{
    private static readonly FrozenDictionary<string, FrozenDictionary<string, string>> Matrix =
        new Dictionary<string, FrozenDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Microsoft.EntityFrameworkCore"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net6.0"] = "6.0.36",
                ["net7.0"] = "7.0.20",
                ["net8.0"] = "8.0.13",
                ["net9.0"] = "9.0.2",
                ["net10.0"] = "10.0.0"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),

            ["Microsoft.EntityFrameworkCore.SqlServer"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net6.0"] = "6.0.36",
                ["net7.0"] = "7.0.20",
                ["net8.0"] = "8.0.13",
                ["net9.0"] = "9.0.2",
                ["net10.0"] = "10.0.0"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),

            ["Microsoft.EntityFrameworkCore.Design"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net6.0"] = "6.0.36",
                ["net7.0"] = "7.0.20",
                ["net8.0"] = "8.0.13",
                ["net9.0"] = "9.0.2",
                ["net10.0"] = "10.0.0"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),

            ["Microsoft.EntityFrameworkCore.InMemory"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net6.0"] = "6.0.36",
                ["net7.0"] = "7.0.20",
                ["net8.0"] = "8.0.13",
                ["net9.0"] = "9.0.2",
                ["net10.0"] = "10.0.0"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),

            ["Microsoft.AspNetCore.Authentication.JwtBearer"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net6.0"] = "6.0.36",
                ["net7.0"] = "7.0.20",
                ["net8.0"] = "8.0.13",
                ["net9.0"] = "9.0.2",
                ["net10.0"] = "10.0.0"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),

            ["Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net6.0"] = "6.0.36",
                ["net7.0"] = "7.0.20",
                ["net8.0"] = "8.0.13",
                ["net9.0"] = "9.0.2",
                ["net10.0"] = "10.0.0"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),

            ["Microsoft.AspNetCore.Mvc.Testing"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net6.0"] = "6.0.36",
                ["net7.0"] = "7.0.20",
                ["net8.0"] = "8.0.13",
                ["net9.0"] = "9.0.2",
                ["net10.0"] = "10.0.0"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),

            ["Microsoft.AspNetCore.OpenApi"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net7.0"] = "7.0.20",
                ["net8.0"] = "8.0.13",
                ["net9.0"] = "9.0.2",
                ["net10.0"] = "10.0.12"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),

            ["Npgsql.EntityFrameworkCore.PostgreSQL"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net6.0"] = "6.0.29",
                ["net7.0"] = "7.0.18",
                ["net8.0"] = "8.0.11",
                ["net9.0"] = "9.0.3",
                ["net10.0"] = "10.0.0"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),

            ["Swashbuckle.AspNetCore"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net6.0"] = "6.6.2",
                ["net7.0"] = "6.6.2",
                ["net8.0"] = "7.2.0",
                ["net9.0"] = "7.2.0",
                ["net10.0"] = "7.2.0"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),

            ["Serilog.AspNetCore"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net6.0"] = "6.1.0",
                ["net7.0"] = "7.0.0",
                ["net8.0"] = "8.0.3",
                ["net9.0"] = "9.0.0",
                ["net10.0"] = "9.0.0"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),

            ["FluentValidation"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net6.0"] = "11.11.0",
                ["net7.0"] = "11.11.0",
                ["net8.0"] = "11.11.0",
                ["net9.0"] = "11.11.0",
                ["net10.0"] = "11.11.0"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),

            ["FluentValidation.DependencyInjectionExtensions"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["net6.0"] = "11.11.0",
                ["net7.0"] = "11.11.0",
                ["net8.0"] = "11.11.0",
                ["net9.0"] = "11.11.0",
                ["net10.0"] = "11.11.0"
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase)
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, IReadOnlyDictionary<string, string>> PrecomputedFrameworkPackages =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["net6.0"] = BuildPackageMap("net6.0"),
            ["net7.0"] = BuildPackageMap("net7.0"),
            ["net8.0"] = BuildPackageMap("net8.0"),
            ["net9.0"] = BuildPackageMap("net9.0"),
            ["net10.0"] = BuildPackageMap("net10.0")
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static bool IsKnownPackage(string packageId) =>
        Matrix.ContainsKey(packageId);

    public static bool HasTargetVersion(string packageId, string targetFramework) =>
        Matrix.TryGetValue(packageId, out var versions) && versions.ContainsKey(targetFramework);

    public static string? GetTargetVersion(string packageId, string targetFramework)
    {
        if (Matrix.TryGetValue(packageId, out var versions) && versions.TryGetValue(targetFramework, out var version))
        {
            return version;
        }

        return null;
    }

    public static IReadOnlyDictionary<string, string> GetCompatiblePackages(string targetFramework)
    {
        if (PrecomputedFrameworkPackages.TryGetValue(targetFramework, out var precomputed))
        {
            return precomputed;
        }

        return BuildPackageMap(targetFramework);
    }

    private static FrozenDictionary<string, string> BuildPackageMap(string targetFramework)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (pkg, versions) in Matrix)
        {
            if (versions.TryGetValue(targetFramework, out var version))
            {
                result[pkg] = version;
            }
        }

        return result.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
