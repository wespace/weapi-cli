namespace WeApi.Cli.Common;

public record ProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError)
{
    public bool Success => ExitCode == 0;
}
