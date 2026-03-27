namespace ClawMailCalCli.Models;

/// <summary>
/// Represents a structured error result written to stderr when <c>--json</c> mode is active.
/// </summary>
/// <param name="Error">A human-readable description of the error.</param>
/// <param name="Code">A machine-readable error code string (see <see cref="ErrorCodes"/>).</param>
public record ErrorResult(string Error, string Code);
