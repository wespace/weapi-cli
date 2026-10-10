using System.Security.Cryptography;
using System.Text;
using WeApi.Cli.Common;

namespace WeApi.Cli.Services;

public static class RollbackManager
{
    public static async Task<bool> ExecuteRollbackAsync(
        IReadOnlyList<FileMigrationChange> changes,
        CancellationToken cancellationToken = default)
    {
        ConsoleUi.WriteWarning("Rolling back file modifications...");
        var allVerified = true;

        foreach (var change in changes)
        {
            try
            {
                if (change.ExistedBefore && change.OriginalContent is not null)
                {
                    // Restore original content
                    await File.WriteAllTextAsync(change.FilePath, change.OriginalContent, cancellationToken);

                    // Compute SHA-256 hashes to guarantee exact restoration
                    var expectedHash = ComputeSha256(change.OriginalContent);
                    var actualDiskContent = await File.ReadAllTextAsync(change.FilePath, cancellationToken);
                    var actualHash = ComputeSha256(actualDiskContent);

                    if (expectedHash != actualHash)
                    {
                        ConsoleUi.WriteError($"Hash mismatch after restoring '{Path.GetFileName(change.FilePath)}'.");
                        allVerified = false;
                    }
                }
                else if (!change.ExistedBefore)
                {
                    // File was newly created during migration - remove it cleanly
                    if (File.Exists(change.FilePath))
                    {
                        File.Delete(change.FilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleUi.WriteError($"Failed to rollback '{Path.GetFileName(change.FilePath)}': {ex.Message}");
                allVerified = false;
            }
        }

        if (allVerified)
        {
            ConsoleUi.WriteStep("All modified files rolled back and verified successfully");
        }

        return allVerified;
    }

    public static string ComputeSha256(string content)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(content.Length);
        if (maxByteCount <= 2048)
        {
            Span<byte> utf8Bytes = stackalloc byte[maxByteCount];
            var written = Encoding.UTF8.GetBytes(content, utf8Bytes);
            Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(utf8Bytes[..written], hashBytes);
            return Convert.ToHexString(hashBytes);
        }

        var bytes = Encoding.UTF8.GetBytes(content);
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(bytes, hash);
        return Convert.ToHexString(hash);
    }
}
