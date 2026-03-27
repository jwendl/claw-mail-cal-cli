namespace ClawMailCalCli.Models;

/// <summary>
/// Represents the result of a mutation command (add, delete, set, send, create, login).
/// </summary>
/// <param name="Success">Whether the operation completed successfully.</param>
/// <param name="Message">A human-readable description of the outcome.</param>
/// <param name="Id">An optional identifier for the created resource (e.g. a calendar event ID).</param>
public record CommandResult(bool Success, string Message, string? Id = null);
