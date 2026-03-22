namespace ClawMailCalCli.Models;

/// <summary>
/// Represents a named Microsoft account stored in the CLI.
/// </summary>
/// <param name="Name">The unique short name used to identify this account on the command line.</param>
/// <param name="Email">The email address associated with this account.</param>
/// <param name="Type">Whether this is a personal (MSA) or work/school (Entra) account.</param>
public record Account(string Name, string Email, AccountType Type);