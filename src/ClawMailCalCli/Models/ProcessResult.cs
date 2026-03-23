namespace ClawMailCalCli.Models;

/// <summary>
/// Represents the result of running an external process.
/// </summary>
/// <param name="ExitCode">The exit code returned by the process.</param>
/// <param name="StandardOutput">The captured standard output of the process.</param>
/// <param name="StandardError">The captured standard error of the process.</param>
public record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
