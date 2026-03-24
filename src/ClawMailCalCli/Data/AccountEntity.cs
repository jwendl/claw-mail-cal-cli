using ClawMailCalCli.Models;

namespace ClawMailCalCli.Data;

/// <summary>
/// Represents an email/calendar account stored in the local SQLite database.
/// </summary>
public class AccountEntity
{
	/// <summary>
	/// The primary key for the account.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// The normalized (lowercased, trimmed) account name.
	/// </summary>
	public required string Name { get; set; }

	/// <summary>
	/// The email address associated with the account.
	/// </summary>
	public required string Email { get; set; }

	/// <summary>
	/// Whether this is a personal (MSA) or work/school (Entra) account.
	/// </summary>
	public AccountType Type { get; set; }

	/// <summary>
	/// Whether this account is set as the default.
	/// </summary>
	public bool IsDefault { get; set; }
}
