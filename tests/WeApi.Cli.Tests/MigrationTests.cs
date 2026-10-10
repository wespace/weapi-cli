using WeApi.Cli.Common;
using WeApi.Cli.Services;
using Xunit;

namespace WeApi.Cli.Tests;

public class MigrationTests
{
    [Theory]
    [InlineData("6", "net6.0")]
    [InlineData("6.0", "net6.0")]
    [InlineData("net6", "net6.0")]
    [InlineData("net6.0", "net6.0")]
    [InlineData("7", "net7.0")]
    [InlineData("7.0", "net7.0")]
    [InlineData("net7", "net7.0")]
    [InlineData("net7.0", "net7.0")]
    [InlineData("8", "net8.0")]
    [InlineData("8.0", "net8.0")]
    [InlineData("net8", "net8.0")]
    [InlineData("net8.0", "net8.0")]
    [InlineData("9", "net9.0")]
    [InlineData("9.0", "net9.0")]
    [InlineData("net9", "net9.0")]
    [InlineData("net9.0", "net9.0")]
    [InlineData("10", "net10.0")]
    [InlineData("10.0", "net10.0")]
    [InlineData("net10", "net10.0")]
    [InlineData("net10.0", "net10.0")]
    public void FrameworkNormalizer_ValidInputs_ShouldNormalizeCorrectly(string input, string expected)
    {
        var success = FrameworkNormalizer.TryNormalize(input, out var normalized);
        Assert.True(success);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("5.0")]
    [InlineData("net5.0")]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void FrameworkNormalizer_InvalidInputs_ShouldReturnFalse(string? input)
    {
        var success = FrameworkNormalizer.TryNormalize(input, out var normalized);
        Assert.False(success);
        Assert.Empty(normalized);
    }

    [Fact]
    public void PackageCompatibility_KnownPackages_ShouldReturnTargetVersions()
    {
        Assert.True(PackageCompatibility.IsKnownPackage("Microsoft.EntityFrameworkCore"));
        Assert.Equal("6.0.36", PackageCompatibility.GetTargetVersion("Microsoft.EntityFrameworkCore", "net6.0"));
        Assert.Equal("7.0.20", PackageCompatibility.GetTargetVersion("Microsoft.EntityFrameworkCore", "net7.0"));
        Assert.Equal("8.0.13", PackageCompatibility.GetTargetVersion("Microsoft.EntityFrameworkCore", "net8.0"));
        Assert.Equal("9.0.2", PackageCompatibility.GetTargetVersion("Microsoft.EntityFrameworkCore", "net9.0"));
        Assert.Equal("10.0.0", PackageCompatibility.GetTargetVersion("Microsoft.EntityFrameworkCore", "net10.0"));
    }

    [Fact]
    public void PackageCompatibility_UnknownPackage_ShouldReturnNull()
    {
        Assert.False(PackageCompatibility.IsKnownPackage("Random.ThirdParty.Package"));
        Assert.Null(PackageCompatibility.GetTargetVersion("Random.ThirdParty.Package", "net10.0"));
    }

    [Theory]
    [InlineData("net6.0")]
    [InlineData("net7.0")]
    [InlineData("net8.0")]
    [InlineData("net9.0")]
    [InlineData("net10.0")]
    public void PackageCompatibility_GetCompatiblePackages_ShouldReturnAllKnownPackages(string framework)
    {
        var packages = PackageCompatibility.GetCompatiblePackages(framework);
        Assert.NotEmpty(packages);
        Assert.True(packages.ContainsKey("Microsoft.EntityFrameworkCore"));
        Assert.True(packages.ContainsKey("Microsoft.AspNetCore.Authentication.JwtBearer"));
    }

    [Theory]
    [InlineData("net6.0", 6)]
    [InlineData("net6", 6)]
    [InlineData("6", 6)]
    [InlineData("net7.0", 7)]
    [InlineData("net7", 7)]
    [InlineData("7", 7)]
    [InlineData("net8.0", 8)]
    [InlineData("net8", 8)]
    [InlineData("8", 8)]
    [InlineData("net9.0", 9)]
    [InlineData("net9", 9)]
    [InlineData("9", 9)]
    [InlineData("net10.0", 10)]
    [InlineData("net10", 10)]
    [InlineData("10", 10)]
    public void SdkResolver_GetTargetMajor_ShouldParseCorrectly(string framework, int expectedMajor)
    {
        var major = SdkResolver.GetTargetMajor(framework);
        Assert.Equal(expectedMajor, major);
    }

    [Fact]
    public void SdkResolver_ParseVersionSafe_ShouldOrderVersionsSemantically()
    {
        var v10 = SdkResolver.ParseVersionSafe("10.0.100");
        var v9 = SdkResolver.ParseVersionSafe("9.0.200");
        var v8 = SdkResolver.ParseVersionSafe("8.0.406");
        var v10Preview = SdkResolver.ParseVersionSafe("10.0.100-preview.1");

        Assert.True(v10 > v9);
        Assert.True(v9 > v8);
        Assert.Equal(v10, v10Preview);
    }

    [Fact]
    public async Task RollbackManager_ShouldRestoreOriginalContentAndVerifyHash()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"weapi_rollback_{Guid.NewGuid()}.txt");
        var originalContent = "Original file content before migration";
        var modifiedContent = "Modified content that should be rolled back";
        await File.WriteAllTextAsync(tempFile, modifiedContent);

        var changes = new List<FileMigrationChange>
        {
            new(
                FilePath: tempFile,
                ExistedBefore: true,
                OriginalContent: originalContent,
                UpdatedContent: modifiedContent,
                Description: "Test change")
        };

        try
        {
            // Act
            var success = await RollbackManager.ExecuteRollbackAsync(changes);

            // Assert
            Assert.True(success);
            var contentOnDisk = await File.ReadAllTextAsync(tempFile);
            Assert.Equal(originalContent, contentOnDisk);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void RollbackManager_ComputeSha256_SmallAndLargeInputs_ShouldProduceCorrectHash()
    {
        // Small input (< 2048 bytes, hits stackalloc branch)
        var smallInput = "Hello, WeApi!";
        var smallHash = RollbackManager.ComputeSha256(smallInput);
        Assert.NotEmpty(smallHash);
        Assert.Equal(64, smallHash.Length);

        // Large input (> 2048 bytes, hits heap fallback branch)
        var largeInput = new string('A', 5000);
        var largeHash = RollbackManager.ComputeSha256(largeInput);
        Assert.NotEmpty(largeHash);
        Assert.Equal(64, largeHash.Length);

        // Deterministic check
        Assert.Equal(smallHash, RollbackManager.ComputeSha256(smallInput));
        Assert.Equal(largeHash, RollbackManager.ComputeSha256(largeInput));
    }

    [Fact]
    public async Task RollbackManager_WhenFileWasNewlyCreated_ShouldDeleteOnRollback()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"weapi_newfile_{Guid.NewGuid()}.txt");
        await File.WriteAllTextAsync(tempFile, "Newly created file content");

        var changes = new List<FileMigrationChange>
        {
            new(
                FilePath: tempFile,
                ExistedBefore: false,
                OriginalContent: null,
                UpdatedContent: "Newly created file content",
                Description: "Newly created file")
        };

        try
        {
            // Act
            var success = await RollbackManager.ExecuteRollbackAsync(changes);

            // Assert
            Assert.True(success);
            Assert.False(File.Exists(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task Upgrade_WithDryRun_ShouldModifyZeroFiles()
    {
        // Arrange
        var projectName = "DryRunApp.Api";
        var baseDir = Path.Combine(Path.GetTempPath(), $"weapi_dryrun_{Guid.NewGuid()}");
        var targetDir = Path.Combine(baseDir, projectName);

        try
        {
            // 1. Scaffold project in net10.0
            var exitCode = await Program.Main(["new", projectName, "-o", targetDir, "-f", "net10.0"]);
            Assert.Equal(0, exitCode);

            var propsPath = Path.Combine(targetDir, "Directory.Build.props");
            Assert.True(File.Exists(propsPath));
            var originalProps = await File.ReadAllTextAsync(propsPath);
            Assert.Contains("<TargetFramework>net10.0</TargetFramework>", originalProps);

            // 2. Run upgrade with --dry-run
            var upgradeExitCode = await Program.Main(["upgrade", targetDir, "-f", "net8.0", "--dry-run", "--diff"]);
            Assert.Equal(0, upgradeExitCode);

            // 3. Verify file was NOT modified on disk
            var propsAfterDryRun = await File.ReadAllTextAsync(propsPath);
            Assert.Equal(originalProps, propsAfterDryRun);
        }
        finally
        {
            if (Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Upgrade_WhenAlreadyTargetingFramework_ShouldReturnZeroImmediately()
    {
        // Arrange
        var projectName = "AlreadyTargetingApp.Api";
        var baseDir = Path.Combine(Path.GetTempPath(), $"weapi_already_{Guid.NewGuid()}");
        var targetDir = Path.Combine(baseDir, projectName);

        try
        {
            // Scaffold project in net10.0
            var exitCode = await Program.Main(["new", projectName, "-o", targetDir, "-f", "net10.0"]);
            Assert.Equal(0, exitCode);

            // Upgrade to net10.0 (already targeting)
            var upgradeExitCode = await Program.Main(["upgrade", targetDir, "-f", "net10.0"]);
            Assert.Equal(0, upgradeExitCode);
        }
        finally
        {
            if (Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Upgrade_WithMultiTargetingComplexity_ShouldFailSafelyWithNoChanges()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"weapi_multi_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var csprojPath = Path.Combine(tempDir, "MultiTarget.csproj");
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
  </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(csprojPath, csprojContent);

        try
        {
            // Act: Attempt to upgrade multi-targeting project
            var exitCode = await Program.Main(["upgrade", tempDir, "-f", "net10.0", "--yes"]);

            // Assert: Should abort safely with exit code 1
            Assert.Equal(1, exitCode);
            var contentAfter = await File.ReadAllTextAsync(csprojPath);
            Assert.Equal(csprojContent, contentAfter);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Upgrade_WeApiProject_BetweenNet10AndNet8_ShouldSucceed()
    {
        // Arrange
        var projectName = "MigrateTestApp.Api";
        var baseDir = Path.Combine(Path.GetTempPath(), $"weapi_mig_{Guid.NewGuid()}");
        var targetDir = Path.Combine(baseDir, projectName);

        try
        {
            // 1. Scaffold project in net10.0
            var newExitCode = await Program.Main(["new", projectName, "-o", targetDir, "-f", "net10.0"]);
            Assert.Equal(0, newExitCode);

            var propsPath = Path.Combine(targetDir, "Directory.Build.props");
            Assert.True(File.Exists(propsPath));
            Assert.Contains("<TargetFramework>net10.0</TargetFramework>", await File.ReadAllTextAsync(propsPath));

            // 2. Upgrade to net8.0 with --yes
            var upgradeExitCode = await Program.Main(["upgrade", targetDir, "-f", "net8.0", "--yes"]);
            Assert.Equal(0, upgradeExitCode);

            // 3. Verify Directory.Build.props TargetFramework was updated
            var propsAfter = await File.ReadAllTextAsync(propsPath);
            Assert.Contains("<TargetFramework>net8.0</TargetFramework>", propsAfter);
            Assert.DoesNotContain("<TargetFramework>net10.0</TargetFramework>", propsAfter);

            // 4. Upgrade back to net10.0 with --yes
            var upgradeBackCode = await Program.Main(["upgrade", targetDir, "-f", "net10.0", "--yes"]);
            Assert.Equal(0, upgradeBackCode);

            // 5. Verify TargetFramework is net10.0 again
            var propsFinal = await File.ReadAllTextAsync(propsPath);
            Assert.Contains("<TargetFramework>net10.0</TargetFramework>", propsFinal);
        }
        finally
        {
            if (Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Upgrade_WithoutGitRepository_ShouldMigrateNormally()
    {
        // Arrange: Create project in temp folder outside git repo
        var projectName = "NoGitApp.Api";
        var baseDir = Path.Combine(Path.GetTempPath(), $"weapi_nogit_{Guid.NewGuid()}");
        var targetDir = Path.Combine(baseDir, projectName);

        try
        {
            var newExitCode = await Program.Main(["new", projectName, "-o", targetDir, "-f", "net10.0"]);
            Assert.Equal(0, newExitCode);

            // Verify GitHelper confirms it's not a git repository
            var gitStatus = await GitHelper.CheckStatusAsync(targetDir);
            Assert.Null(gitStatus.GitRoot);
            Assert.False(gitStatus.IsGitRepository);

            // Act: Run upgrade on non-git directory
            var upgradeExitCode = await Program.Main(["upgrade", targetDir, "-f", "net9.0", "--yes"]);

            // Assert: Migration succeeded normally
            Assert.Equal(0, upgradeExitCode);
            var propsPath = Path.Combine(targetDir, "Directory.Build.props");
            var propsContent = await File.ReadAllTextAsync(propsPath);
            Assert.Contains("<TargetFramework>net9.0</TargetFramework>", propsContent);
        }
        finally
        {
            if (Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Upgrade_LegacyNonSdkProject_ShouldFailSafelyWithNoChanges()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"weapi_legacy_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var csprojPath = Path.Combine(tempDir, "LegacyApp.csproj");
        var legacyContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
  </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(csprojPath, legacyContent);

        try
        {
            // Act
            var exitCode = await Program.Main(["upgrade", tempDir, "-f", "net10.0", "--yes"]);

            // Assert: Aborts safely
            Assert.Equal(1, exitCode);
            var contentAfter = await File.ReadAllTextAsync(csprojPath);
            Assert.Equal(legacyContent, contentAfter);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Upgrade_AmbiguousMismatchedProjects_ShouldFailSafelyWithNoChanges()
    {
        // Arrange: Two projects targeting different frameworks without Directory.Build.props
        var tempDir = Path.Combine(Path.GetTempPath(), $"weapi_mismatch_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var proj1 = Path.Combine(tempDir, "ProjectA.csproj");
        var proj2 = Path.Combine(tempDir, "ProjectB.csproj");

        var proj1Content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";
        var proj2Content = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>";

        await File.WriteAllTextAsync(proj1, proj1Content);
        await File.WriteAllTextAsync(proj2, proj2Content);

        try
        {
            // Act
            var exitCode = await Program.Main(["upgrade", tempDir, "-f", "net10.0", "--yes"]);

            // Assert: Aborts safely due to ambiguous structure
            Assert.Equal(1, exitCode);
            Assert.Equal(proj1Content, await File.ReadAllTextAsync(proj1));
            Assert.Equal(proj2Content, await File.ReadAllTextAsync(proj2));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Upgrade_GenericSdkProject_ShouldUpdateTargetFrameworkAndWhitelistedPackagesOnly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"weapi_generic_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var projPath = Path.Combine(tempDir, "GenericApp.csproj");

        var originalContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""8.0.13"" />
    <PackageReference Include=""MyCompany.CustomPackage"" Version=""1.2.3"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(projPath, originalContent);

        try
        {
            // Act: Dry-run upgrade to net10.0
            var dryRunCode = await Program.Main(["upgrade", tempDir, "-f", "net10.0", "--dry-run"]);
            Assert.Equal(0, dryRunCode);
            Assert.Equal(originalContent, await File.ReadAllTextAsync(projPath));

            // Verify with analyze directly
            var discovery = ProjectDiscovery.Discover(tempDir, null)!;
            var plan = await MigrationAnalyzer.AnalyzeAsync(discovery, null, "net10.0", false);

            Assert.True(plan.IsSupported);
            Assert.Equal(ProjectType.GenericSdkProject, plan.ProjectType);
            Assert.Single(plan.FileChanges);

            var updatedContent = plan.FileChanges[0].UpdatedContent;
            Assert.Contains("<TargetFramework>net10.0</TargetFramework>", updatedContent);
            Assert.Contains(@"<PackageReference Include=""Microsoft.EntityFrameworkCore"" Version=""10.0.0"" />", updatedContent);
            // User package MUST remain completely untouched
            Assert.Contains(@"<PackageReference Include=""MyCompany.CustomPackage"" Version=""1.2.3"" />", updatedContent);
            // Warning emitted for unknown package
            Assert.Contains(plan.Warnings, w => w.Contains("MyCompany.CustomPackage"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
