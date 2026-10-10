using WeApi.Cli.Services;
using Xunit;

namespace WeApi.Cli.Tests;

public class CliTests
{
    [Fact]
    public async Task Main_WithHelpFlag_ShouldReturnZero()
    {
        // Act
        var exitCode = await Program.Main(["--help"]);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_NewWithHelpFlag_ShouldReturnZero()
    {
        // Act
        var exitCode = await Program.Main(["new", "--help"]);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_UpgradeWithHelpFlag_ShouldReturnZero()
    {
        // Act
        var exitCode = await Program.Main(["upgrade", "--help"]);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("-?")]
    [InlineData("help")]
    [InlineData("/help")]
    [InlineData("/h")]
    [InlineData("/?")]
    public async Task Main_WithHelpFlags_ShouldReturnZero(string flag)
    {
        // Act
        var exitCode = await Program.Main([flag]);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-v")]
    [InlineData("version")]
    [InlineData("/version")]
    [InlineData("/v")]
    public async Task Main_WithVersionFlags_ShouldReturnZero(string flag)
    {
        // Act
        var exitCode = await Program.Main([flag]);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Theory]
    [InlineData("123-Invalid")]
    [InlineData("My Project With Spaces")]
    [InlineData("My-Project-With-Dashes")]
    [InlineData("!SpecialChars@")]
    public async Task Main_WithInvalidProjectName_ShouldReturnOne(string invalidName)
    {
        // Act
        var exitCode = await Program.Main(["new", invalidName]);

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Main_WithMissingProjectName_ShouldReturnOne()
    {
        // Act
        var exitCode = await Program.Main(["new"]);

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Main_WithUnsupportedDatabase_ShouldReturnOne()
    {
        // Act
        var exitCode = await Program.Main(["new", "ValidName", "--database", "oracle"]);

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Main_WithUnsupportedIdType_ShouldReturnOne()
    {
        // Act
        var exitCode = await Program.Main(["new", "ValidName", "--id-type", "custom"]);

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Main_WithDryRun_ShouldReturnZero()
    {
        // Act
        var exitCode = await Program.Main(["new", "ValidName", "--dry-run"]);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Main_WhenDirectoryAlreadyContainsFiles_ShouldReturnOne()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"weapi_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var existingFile = Path.Combine(tempDir, "existing.txt");
        await File.WriteAllTextAsync(existingFile, "Existing file content");

        try
        {
            // Act
            var exitCode = await Program.Main(["new", "ValidName", "-o", tempDir]);

            // Assert
            Assert.Equal(1, exitCode);
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
    public async Task Main_WithValidProject_ShouldGenerateFullSolution()
    {
        // Arrange
        var projectName = "TestGenApp.Store";
        var baseDir = Path.Combine(Path.GetTempPath(), $"weapi_gen_{Guid.NewGuid()}");
        var targetDir = Path.Combine(baseDir, projectName);

        try
        {
            // Act
            var exitCode = await Program.Main([
                "new",
                projectName,
                "-o",
                targetDir,
                "--database",
                "postgresql"
            ]);

            // Assert
            Assert.Equal(0, exitCode);
            Assert.True(Directory.Exists(targetDir));

            // Verify solution and project files exist
            var slnxFile = Path.Combine(targetDir, $"{projectName}.slnx");
            Assert.True(File.Exists(slnxFile));

            Assert.True(Directory.Exists(Path.Combine(targetDir, "src", $"{projectName}.API")));
            Assert.True(Directory.Exists(Path.Combine(targetDir, "src", $"{projectName}.Application")));
            Assert.True(Directory.Exists(Path.Combine(targetDir, "src", $"{projectName}.Domain")));
            Assert.True(Directory.Exists(Path.Combine(targetDir, "src", $"{projectName}.Infrastructure")));
            Assert.True(Directory.Exists(Path.Combine(targetDir, "tests", $"{projectName}.UnitTests")));
            Assert.True(Directory.Exists(Path.Combine(targetDir, "tests", $"{projectName}.IntegrationTests")));

            // Verify configuration in appsettings.json
            var appsettingsPath = Path.Combine(targetDir, "src", $"{projectName}.API", "appsettings.json");
            Assert.True(File.Exists(appsettingsPath));
            var appsettingsContent = await File.ReadAllTextAsync(appsettingsPath);
            Assert.Contains("\"Provider\": \"PostgreSql\"", appsettingsContent);
            Assert.Contains(projectName, appsettingsContent);
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
    public async Task Main_WithIntIdType_ShouldGenerateValidProjectWithIntIds()
    {
        // Arrange
        var projectName = "TestGenInt.Api";
        var baseDir = Path.Combine(Path.GetTempPath(), $"weapi_gen_{Guid.NewGuid()}");
        var targetDir = Path.Combine(baseDir, projectName);

        try
        {
            // Act
            var exitCode = await Program.Main([
                "new",
                projectName,
                "-o",
                targetDir,
                "--id-type",
                "int"
            ]);

            // Assert
            Assert.Equal(0, exitCode);
            Assert.True(Directory.Exists(targetDir));

            // Verify BaseEntity uses int Id
            var baseEntityFile = Path.Combine(targetDir, "src", $"{projectName}.Domain", "Common", "BaseEntity.cs");
            Assert.True(File.Exists(baseEntityFile));
            var baseEntityContent = await File.ReadAllTextAsync(baseEntityFile);
            Assert.Contains("public int Id", baseEntityContent);

            // Verify UserResponse uses int Id
            var userResponseFile = Path.Combine(targetDir, "src", $"{projectName}.Application", "DTOs", "Auth", "UserResponse.cs");
            Assert.True(File.Exists(userResponseFile));
            var userResponseContent = await File.ReadAllTextAsync(userResponseFile);
            Assert.Contains("int Id,", userResponseContent);
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
    public async Task Main_WithLongIdType_ShouldGenerateValidProjectWithLongIds()
    {
        // Arrange
        var projectName = "TestGenLong.Api";
        var baseDir = Path.Combine(Path.GetTempPath(), $"weapi_gen_{Guid.NewGuid()}");
        var targetDir = Path.Combine(baseDir, projectName);

        try
        {
            // Act
            var exitCode = await Program.Main([
                "new",
                projectName,
                "-o",
                targetDir,
                "--id-type",
                "long"
            ]);

            // Assert
            Assert.Equal(0, exitCode);
            Assert.True(Directory.Exists(targetDir));

            // Verify BaseEntity uses long Id
            var baseEntityFile = Path.Combine(targetDir, "src", $"{projectName}.Domain", "Common", "BaseEntity.cs");
            Assert.True(File.Exists(baseEntityFile));
            var baseEntityContent = await File.ReadAllTextAsync(baseEntityFile);
            Assert.Contains("public long Id", baseEntityContent);

            // Verify UserResponse uses long Id
            var userResponseFile = Path.Combine(targetDir, "src", $"{projectName}.Application", "DTOs", "Auth", "UserResponse.cs");
            Assert.True(File.Exists(userResponseFile));
            var userResponseContent = await File.ReadAllTextAsync(userResponseFile);
            Assert.Contains("long Id,", userResponseContent);
        }
        finally
        {
            if (Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, recursive: true);
            }
        }
    }
}
