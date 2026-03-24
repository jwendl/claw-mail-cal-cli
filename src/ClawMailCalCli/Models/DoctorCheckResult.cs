namespace ClawMailCalCli.Models;

/// <summary>
/// Represents the result of a single doctor environment check.
/// </summary>
/// <param name="CheckName">The human-readable name of the check.</param>
/// <param name="Passed">Whether the check passed.</param>
/// <param name="Message">A detail message shown alongside the check result.</param>
/// <param name="FixHint">An optional hint explaining how to resolve a failure.</param>
public record DoctorCheckResult(string CheckName, bool Passed, string Message, string? FixHint = null);
